using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace AutoStock.Repositories
{
    public interface ICustomerRepository<T> where T : class
    {
         IQueryable<T> GetAll();
         IQueryable<T> Where(Expression<Func<T, bool>> predicate);
         ValueTask<T> GetByIdAsync(int id);
         ValueTask AddAsync(T entity);
         void Update(T entity);
         void Delete(T entity);
    }
}
