using System.Linq.Expressions;
using SafeTrace.Domain.Specifications.Expressions;

namespace SafeTrace.Domain.Specifications {
    public interface ISpecification<T> where T : class
    {
        List<Expression<Func<T, bool>>> CriteriaList { get; }
        List<Expression<Func<T, object>>> Includes { get; }
        List<OrderExpression<T>> OrderExpressions { get; }
        int Skip { get; }
        int Take { get; }
        bool IsPagingEnabled { get; }
        bool IsTracking { get; }
    }
}

