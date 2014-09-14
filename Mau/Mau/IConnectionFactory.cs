using System.Data;

namespace Mau
{
    public interface IConnectionFactory
    {
        IDbConnection Create();
    }
}
