using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories
{
    public class UnitOfWork(AppDbContext context): IUnitOfWork
    {   
        private readonly AppDbContext _context;

        
        public Task<int> SaveChangesAsync()=> _context.SaveChangesAsync();
        
    }
}
