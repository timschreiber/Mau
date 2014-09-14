using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Mau
{
    public class AdoNetContext : IAdoNetContext
    {
        #region Fields
        private readonly IDbConnection _connection;
        private readonly IConnectionFactory _connectionFactory;
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        private readonly LinkedList<AdoNetUnitOfWork> _uows = new LinkedList<AdoNetUnitOfWork>();
        private readonly IDictionary<Type, DbType> _dbTypeMap = new Dictionary<Type, DbType>()
        {
            { typeof(byte), DbType.Byte },
            { typeof(sbyte), DbType.SByte },
            { typeof(short), DbType.Int16 },
            { typeof(ushort), DbType.UInt16 },
            { typeof(int), DbType.Int32 },
            { typeof(uint), DbType.UInt32 },
            { typeof(long), DbType.Int64 },
            { typeof(ulong), DbType.UInt64 },
            { typeof(float), DbType.Single },
            { typeof(double), DbType.Double },
            { typeof(decimal), DbType.Decimal },
            { typeof(bool), DbType.Boolean },
            { typeof(string), DbType.String },
            { typeof(char), DbType.StringFixedLength },
            { typeof(Guid), DbType.Guid },
            { typeof(DateTime), DbType.DateTime },
            { typeof(DateTimeOffset), DbType.DateTimeOffset },
            { typeof(byte[]), DbType.Binary },
            { typeof(byte?), DbType.Byte },
            { typeof(sbyte?), DbType.SByte },
            { typeof(short?), DbType.Int16 },
            { typeof(ushort?), DbType.UInt16 },
            { typeof(int?), DbType.Int32 },
            { typeof(uint?), DbType.UInt32 },
            { typeof(long?), DbType.Int64 },
            { typeof(ulong?), DbType.UInt64 },
            { typeof(float?), DbType.Single },
            { typeof(double?), DbType.Double },
            { typeof(decimal?), DbType.Decimal },
            { typeof(bool?), DbType.Boolean },
            { typeof(char?), DbType.StringFixedLength },
            { typeof(Guid?), DbType.Guid },
            { typeof(DateTime?), DbType.DateTime },
            { typeof(DateTimeOffset?), DbType.DateTimeOffset }
        };
        #endregion

        #region Constructors
        public AdoNetContext(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _connection = _connectionFactory.Create();
        }
        #endregion

        #region IAdoNetContext Members
        public IUnitOfWork CreateUnitOfWork()
        {
            var transaction = _connection.BeginTransaction();
            var uow = new AdoNetUnitOfWork(transaction, RemoveTransaction, RemoveTransaction);
            _rwLock.EnterWriteLock();
            _uows.AddLast(uow);
            _rwLock.ExitWriteLock();
            return uow;
        }

        public int Execute(string commandText, object parameters = null, CommandType commandType = CommandType.Text)
        {
            var result = default(int);
            using (var command = createCommand(commandText, commandType, parameters))
            {
                result = command.ExecuteNonQuery();
            }
            return result;
        }

        public T Scalar<T>(string commandText, object parameters = null, CommandType commandType = CommandType.Text)
        {
            var result = default(T);
            using (var command = createCommand(commandText, commandType, parameters))
            {
                var temp = command.ExecuteScalar();
                result = (T)Convert.ChangeType(temp, typeof(T));
            }
            return result;
        }

        public IList<dynamic> Query(string commandText, object parameters = null, CommandType commandType = CommandType.Text)
        {
            IList<dynamic> result = new List<dynamic>();
            using (var command = createCommand(commandText, commandType, parameters))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var item = new ExpandoObject() as IDictionary<string, object>;
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var value = default(object);
                        if (!reader.IsDBNull(i))
                        {
                            value = reader.GetValue(i);
                        }
                        item.Add(reader.GetName(i), value);
                    }
                    result.Add(item);
                }
                reader.Close();
            }
            return result;
        }

        public IList<T> Query<T>(string commandText, object parameters = null, CommandType commandType = CommandType.Text)
        {
            var result = new List<T>();
            var propertyInfos = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanWrite);
            using (var command = createCommand(commandText, commandType, parameters))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    T item = Activator.CreateInstance<T>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var propertyInfo = propertyInfos.Where(x => x.Name == reader.GetName(i)).FirstOrDefault();
                        if (propertyInfo != null && !reader.IsDBNull(i))
                        {
                            propertyInfo.SetValue(item, reader.GetValue(i));
                        }
                    }
                    result.Add(item);
                }
                reader.Close();
            }
            return result;
        }
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            _connection.Dispose();
        }
        #endregion

        #region Private Methods
        private IDbCommand createCommand(string commandText, CommandType commandType, object parameters = null)
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandType = commandType;
            addParameters(cmd, parameters);
            _rwLock.EnterReadLock();
            if (_uows.Count > 0)
                cmd.Transaction = _uows.First.Value.Transaction;
            _rwLock.ExitReadLock();

            return cmd;
        }

        private void RemoveTransaction(AdoNetUnitOfWork obj)
        {
            _rwLock.EnterWriteLock();
            _uows.Remove(obj);
            _rwLock.ExitWriteLock();
        }

        private void addParameters(IDbCommand command, object parameters)
        {
            if (parameters != null)
            {
                string parameterMarker = getParameterMarker(command);
                PropertyInfo[] paramPropInfos = parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo propInfo in paramPropInfos)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = string.Format("{0}{1}", parameterMarker, propInfo.Name);

                    var val = propInfo.GetValue(parameters);
                    if (val == null)
                        parameter.Value = DBNull.Value;
                    else
                        parameter.Value = val;

                    if (_dbTypeMap.ContainsKey(propInfo.PropertyType))
                        parameter.DbType = _dbTypeMap[propInfo.PropertyType];

                    command.Parameters.Add(parameter);
                }
            }
        }

        private string getParameterMarker(IDbCommand command)
        {
            var result = default(string);
            var connection = command.Connection;
            if (connection is SqlConnection)
            {
                result = "@";
            }
            else
            {
                var con = connection as DbConnection;
                result = con
                    .GetSchema(DbMetaDataCollectionNames.DataSourceInformation)
                    .Rows[0][DbMetaDataColumnNames.ParameterMarkerFormat].ToString();
            }
            return result;
        }
        #endregion
    }
}
