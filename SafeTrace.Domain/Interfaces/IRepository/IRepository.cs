using SafeTrace.Domain.Specifications;

namespace SafeTrace.Domain.Interfaces.IRepository
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetAsync(ISpecification<T> spec);
        Task<IReadOnlyList<T>> GetAllAsync(ISpecification<T> spec);
        Task<T?> GetByIdAsync(object id);
        Task CreateAsync(T entity);
        Task CreateRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);    
        Task<int> CountAsync(ISpecification<T> spec);
        Task<bool> AnyAsync(ISpecification<T> spec);
    }
}