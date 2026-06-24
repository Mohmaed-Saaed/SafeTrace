using System.Linq.Expressions;
using SafeTrace.Domain.Common;

namespace SafeTrace.Domain.Interfaces.IRepository
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> Query(
            bool tracked = true,
            Expression<Func<T, object>>? orderBy = null,
            string orderByDirection = OrderBy.Ascending,
            int? page = null,
            int? pageSize = null,
            params Expression<Func<T, object>>[] includes);

        Task<T?> GetByIdAsync(object id);

        Task<T?> GetOneAsync(Expression<Func<T, bool>> predicate, bool tracked = true, params Expression<Func<T, object>>[] includes);

        Task CreateAsync(T entity);

        Task CreateRangeAsync(IEnumerable<T> entities);

        void Update(T entity);

        void Remove(T entity);

        void RemoveRange(IEnumerable<T> entities);

        Task<bool> AnyAsync();

        Task<int> CountAsync();

    }
}