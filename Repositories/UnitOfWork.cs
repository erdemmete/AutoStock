using AutoStock.Repositories.Interfaces;

namespace AutoStock.Repositories
{
    public class UnitOfWork(AppDbContext context) : IUnitOfWork
    {



        public Task<int> SaveChangesAsync() => context.SaveChangesAsync();

    }
}
