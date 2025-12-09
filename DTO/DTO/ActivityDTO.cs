namespace DTO;

public class ActivityDTO
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = null!;
    public int? CreatedBy { get; set; }
    public int? ApprovedBy { get; set; }
}
