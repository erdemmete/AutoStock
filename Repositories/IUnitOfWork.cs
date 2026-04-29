using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
    }
}
