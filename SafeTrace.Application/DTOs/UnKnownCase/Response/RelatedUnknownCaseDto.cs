namespace SafeTrace.Application.DTOs.UnKnownCase.Response
{
    public class RelatedUnknownCaseDto
    {
    public long Id { get; set; }

    public string CaseCode { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public float Similarity { get; set; }

    public string MainPhotoPath { get; set; } = null!;
    }
}
