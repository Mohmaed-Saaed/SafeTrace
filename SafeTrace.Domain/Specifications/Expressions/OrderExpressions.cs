using System.Linq.Expressions;

namespace SafeTrace.Domain.Specifications.Expressions
{
    public class OrderExpression<T>
    {
        public Expression<Func<T, object>> KeySelector { get; }
        public bool IsDescending { get; }

        public OrderExpression(Expression<Func<T, object>> keySelector, bool isDescending)
        {
            KeySelector = keySelector;
            IsDescending = isDescending;
        }
    }
}

