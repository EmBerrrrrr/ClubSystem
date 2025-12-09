namespace DTO.DTO.Activity;

public class ActivityDto
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Location { get; set; }
    public string Status { get; set; }
    public int? CreatedBy { get; set; }
}

