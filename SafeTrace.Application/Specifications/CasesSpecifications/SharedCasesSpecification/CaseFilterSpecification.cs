using NetTopologySuite.Geometries;
using SafeTrace.Application.DTOs.Cases;

namespace SafeTrace.Application.Specifications.Cases.Shared
{
    public class CaseFilterSpecification<TCase> : BaseSpecification<TCase> where TCase : Case
    {
        public CaseFilterSpecification(CaseFilterDto filter)
        {
            ApplySearch(filter);

            ApplyFilters(filter);

            ApplyNearbyFilter(filter);

            ApplySorting(filter);

            ApplyPaging((filter.PageNumber - 1) * filter.PageSize, filter.PageSize);

            AsNoTracking();
        }

        private void ApplySearch(CaseFilterDto filter)
        {
            if (string.IsNullOrWhiteSpace(filter.Search))
                return;

            var search = filter.Search.Trim().ToLower();

            AddCriteria(x =>

                (x.FName != null && x.FName.Contains(search, StringComparison.CurrentCultureIgnoreCase))

                ||

                (x.SName != null && x.SName.Contains(search, StringComparison.CurrentCultureIgnoreCase))

                ||

                (x.TName != null && x.TName.Contains(search, StringComparison.CurrentCultureIgnoreCase))

                ||

                (x.LName != null && x.LName.Contains(search, StringComparison.CurrentCultureIgnoreCase))
            );
        }

        private void ApplyFilters(CaseFilterDto filter)
        {
            if (filter.Gender.HasValue)
            {
                AddCriteria(x => x.Gender == filter.Gender.Value);
            }

            if (filter.AgeCategoryId.HasValue)
            {
                AddCriteria(x => x.AgeCategoryId == filter.AgeCategoryId.Value);
            }

            if (filter.Status.HasValue)
            {
                AddCriteria(x => x.Status == filter.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Government))
            {
                AddCriteria(x => x.Government == filter.Government);
            }

            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                AddCriteria(x => x.City == filter.City);
            }

            if (filter.CreatedAtFrom.HasValue)
            {
                AddCriteria(x => x.CreatedAt >= filter.CreatedAtFrom.Value);
            }

            if (filter.CreatedAtTo.HasValue)
            {
                AddCriteria(x => x.CreatedAt <= filter.CreatedAtTo.Value);
            }

            if (filter.EventDateFrom.HasValue)
            {
                AddCriteria(x => x.EventDate >= filter.EventDateFrom.Value);
            }

            if (filter.EventDateTo.HasValue)
            {
                AddCriteria(x => x.EventDate <= filter.EventDateTo.Value);
            }
        }

        private void ApplyNearbyFilter(CaseFilterDto filter)
        {
            if (!filter.NearbyOnly)
                return;

            if (!filter.Latitude.HasValue || !filter.Longitude.HasValue)
                return;

            var point = new Point(filter.Longitude.Value, filter.Latitude.Value)
            {
                SRID = 4326
            };

            AddCriteria(x => x.Location.Distance(point) <= filter.RadiusKm * 1000);
        }

        private void ApplySorting(CaseFilterDto filter)
        {
            if (filter.SortByNearest && filter.Latitude.HasValue && filter.Longitude.HasValue)
            {
                var point = new Point(filter.Longitude.Value, filter.Latitude.Value)
                {
                    SRID = 4326
                };

                AddOrderBy(x => x.Location.Distance(point));

                return;
            }

            switch (filter.SortBy?.ToLower())
            {
                case "age":

                    if (filter.IsDescending)
                        AddOrderByDescending(x => x.Age);
                    else
                        AddOrderBy(x => x.Age);

                    break;

                case "eventdate":

                    if (filter.IsDescending)
                        AddOrderByDescending(x => x.EventDate);
                    else
                        AddOrderBy(x => x.EventDate);

                    break;

                case "createdat":

                    if (filter.IsDescending)
                        AddOrderByDescending(x => x.CreatedAt);
                    else
                        AddOrderBy(x => x.CreatedAt);

                    break;

                default:

                    AddOrderByDescending(x => x.CreatedAt);

                    break;
            }
        }
    }
}