using SafeTrace.Domain.Common;
using System.Linq.Expressions;

namespace SafeTrace.Domain.Interfaces.IRepository
{
    public interface IRepository<T> where T : class
    {
        // CRUD
        Task<bool> CreateAsync(T entity);
        Task<bool> CreateRangeAsync(IEnumerable<T> entity);
        Task<bool> UpdateAsync(T entity);
        Task<bool> DeleteAsync(T entity);
        Task<bool> DeleteRangeAsync(IEnumerable<T> entity);
        public Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? expression = null, bool tracked = true, Expression<Func<T, object>>? orderBy = null, string orderByDirection = OrderBy.Ascending,
            int page = 1,
            int pageSize = 10,
            params Expression<Func<T, object>>[] includes);

        Task<T?> GetOneAsync(Expression<Func<T, bool>>? expression = null, bool tracked = true, params Expression<Func<T, object>>[] includes);

        Task<bool> AnyAsync(Expression<Func<T, bool>>? condition = null);
        Task<int> CountAsync(Expression<Func<T, bool>>? expression = null);
        Task AddAsync(T entity);

    }
}
