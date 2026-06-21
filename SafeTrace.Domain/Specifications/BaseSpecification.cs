using System.Linq.Expressions;
using SafeTrace.Domain.Specifications.Expressions;

namespace SafeTrace.Domain.Specifications{
    public abstract class BaseSpecification<T> : ISpecification<T> where T : class
    {
        public List<Expression<Func<T, bool>>> CriteriaList { get; } = [];
        public List<Expression<Func<T, object>>> Includes { get; } = [];
        public List<OrderExpression<T>> OrderExpressions { get; } = [];
        public int Skip { get; private set; }
        public int Take { get; private set; }
        public bool IsPagingEnabled { get; private set; }
        public bool IsTracking { get; private set; } = true;

        protected void AddCriteria(Expression<Func<T, bool>> criteria)
        {
            CriteriaList.Add(criteria);
        }
        protected void AddInclude(Expression<Func<T, object>> include)
        {
            Includes.Add(include);
        }
        protected void AddOrderBy(Expression<Func<T, object>> expression)
        {
            OrderExpressions.Add(new OrderExpression<T>(expression, false));
        }

        protected void AddOrderByDescending(Expression<Func<T, object>> expression)
        {
            OrderExpressions.Add(new OrderExpression<T>(expression, true));
        }

        protected void ApplyPaging(int skip, int take) 
        { 
            Skip = skip; 
            Take = take;
            IsPagingEnabled = true; 
        }
        
        protected void AsNoTracking()
        {
            IsTracking = false;
        }
    }
}
