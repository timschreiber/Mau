using System;
using System.Collections.Generic;
using System.Data;

namespace Mau
{
    public interface IAdoNetContext : IDisposable
    {
        IUnitOfWork CreateUnitOfWork();
        int Execute(string commandText, object parameters = null, CommandType commandType = CommandType.Text);
        T Scalar<T>(string commandText, object parameters = null, CommandType commandType = CommandType.Text);
        IList<dynamic> Query(string commandText, object parameters = null, CommandType commandType = CommandType.Text);
        IList<T> Query<T>(string commandText, object parameters = null, CommandType commandType = CommandType.Text);
    }
}
