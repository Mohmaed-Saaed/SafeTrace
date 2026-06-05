using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SafeTrace.Domain.Interfaces.IReposityory
{
    public interface IRepository<T> where T : class
    {
        // CRUD
        Task<bool> CreateAsync(T entity);
        Task<bool> CreateRangeAsync(IEnumerable<T> entity);
        Task<bool> UpdateAsync(T entity);
        Task<bool> DeleteAsync(T entity);
        Task<bool> DeleteRangeAsync(IEnumerable<T> entity);

        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? expression = null, bool tracked = true,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderByExpression = null, int take = -1, params Expression<Func<T, object>>[] includes);

        Task<T?> GetOneAsync(Expression<Func<T, bool>>? expression = null, bool tracked = true , params Expression<Func<T, object>>[] includes);

        Task<bool> AnyAsync(Expression<Func<T, bool>>? condition = null);
        Task<int> CountAsync(Expression<Func<T, bool>>? expression = null);
    }
}
