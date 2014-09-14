using System;

namespace Mau
{
    public interface IUnitOfWork : IDisposable
    {
        void SaveChanges();
    }
}
