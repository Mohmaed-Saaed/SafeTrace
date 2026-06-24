namespace SafeTrace.Application.DTOs.UrgentMissingCase.Response
{
    public class CasePhotoDto
    {
        public long Id { get; set; }
        public string ImagePath { get; set; } = null!;
        public bool IsPrimary { get; set; } 
        public FileType Type { get; set; }
    }
}