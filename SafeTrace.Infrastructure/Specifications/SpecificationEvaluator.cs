using SafeTrace.Domain.Specifications;
using SafeTrace.Domain.Specifications.Expressions;

namespace SafeTrace.Infrastructure.Specifications {
    public static class SpecificationEvaluator
    {
        public static IQueryable<T> GetQuery<T> (IQueryable<T> query, ISpecification<T> spec) where T : class
        {

            if (spec.CriteriaList.Count > 0)
            {
                foreach (var criteria in spec.CriteriaList)
                {
                    query = query.Where(criteria);
                }
            }

            if (spec.Includes.Count > 0)
            {
                foreach (var include in spec.Includes)
                {
                    query = query.Include(include);
                }
            }

            if (spec.OrderExpressions.Count > 0)
            {
                IOrderedQueryable<T>? ordered = null;

                foreach (var order in spec.OrderExpressions)
                {
                    if (ordered is null)
                    {
                        ordered = order.IsDescending
                            ? query.OrderByDescending(order.KeySelector)
                            : query.OrderBy(order.KeySelector);
                    }
                    else
                    {
                        ordered = order.IsDescending
                            ? ordered.ThenByDescending(order.KeySelector)
                            : ordered.ThenBy(order.KeySelector);
                    }
                }

                query = ordered!;
            }

            if (spec.IsPagingEnabled)
            {
                query = query.Skip(spec.Skip).Take(spec.Take);
            }

            if (!spec.IsTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }
    }
}
