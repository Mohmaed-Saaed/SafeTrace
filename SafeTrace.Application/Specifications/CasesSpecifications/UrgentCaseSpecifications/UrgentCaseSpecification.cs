using NetTopologySuite.Geometries;
using SafeTrace.Application.DTOs.UrgentMissingCase;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Specifications;

public class UrgentCaseSpecification : BaseSpecification<UrgentCase>
{
    public UrgentCaseSpecification(UrgentCaseFilterDto filter)
    {
        AsNoTracking();
        
        AddInclude(x => x.Photos);

        // -------------------------
        // 1. SEARCH
        // -------------------------
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            AddCriteria(x =>
                x.FName.Contains(filter.Search) ||
                x.SName.Contains(filter.Search) ||
                x.LName.Contains(filter.Search) ||
                x.CaseCode.Contains(filter.Search)
            );
        }

        // -------------------------
        // 2. FILTERS
        // -------------------------
        if (filter.Gender.HasValue)
            AddCriteria(x => x.Gender == filter.Gender.Value);

        if (filter.MinAge.HasValue)
            AddCriteria(x => x.Age >= filter.MinAge.Value);

        if (filter.MaxAge.HasValue)
            AddCriteria(x => x.Age <= filter.MaxAge.Value);

        // -------------------------
        // 3. GEO FILTER
        // -------------------------
        Point? point = null;
        double? radiusMeters = null;

        if (filter.Latitude.HasValue && filter.Longitude.HasValue)
        {
            point = new Point(filter.Longitude.Value, filter.Latitude.Value)
            {
                SRID = 4326
            };

            radiusMeters = filter.RadiusKm * 1000;

            AddCriteria(x =>
                x.Location != null &&
                x.Location.Distance(point) <= radiusMeters
            );
        }

        // -------------------------
        // 5. PAGINATION
        // -------------------------
        ApplyPaging((filter.PageNumber - 1) * filter.PageSize, filter.PageSize);
    }
}