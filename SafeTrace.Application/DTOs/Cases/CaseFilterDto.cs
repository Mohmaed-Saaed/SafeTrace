namespace SafeTrace.Application.DTOs.Cases{
    public class CaseFilterDto
    {
        public string? Search { get; set; }
        public Gender? Gender { get; set; }
        public int? AgeCategoryId { get; set; }
        public CaseStatus? Status { get; set; }
        public string? Government { get; set; }
        public string? City { get; set; }
        public DateTime? EventDateFrom { get; set; }
        public DateTime? EventDateTo { get; set; }
        public DateTime? CreatedAtFrom { get; set; }
        public DateTime? CreatedAtTo { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double RadiusKm { get; set; } = 50;
        public bool NearbyOnly { get; set; }
        public bool SortByNearest { get; set; }
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
