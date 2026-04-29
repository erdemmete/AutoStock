using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace AutoStock.Repositories
{
    public class GenericRepository<T>(AppDbContext context) : ICustomerRepository<T> where T : class
    {
        private readonly DbSet<T> _dbSet = context.Set<T>();

        public IQueryable<T> GetAll() => _dbSet.AsQueryable().AsNoTracking();

        public IQueryable<T> Where(Expression<Func<T, bool>> predicate) => _dbSet.Where(predicate);
        public ValueTask<T> GetByIdAsync(int id) => _dbSet.FindAsync(id);

        public async ValueTask AddAsync(T entity)=> await _dbSet.AddAsync(entity);

        public void Update(T entity) => _dbSet.Update(entity);


        public void Delete(T entity) => _dbSet.Remove(entity);
        

        

       

        

        
    }
}
