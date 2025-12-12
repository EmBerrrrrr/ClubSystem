namespace DTO.DTO.Activity;

public class ActivityDto
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? CreatedBy { get; set; }
    public string? ImageActsUrl { get; set; }
    public string? AvatarPublicId { get; set; }
}

