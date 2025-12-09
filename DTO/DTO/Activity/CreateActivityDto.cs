namespace DTO.DTO.Activity;

public class CreateActivityDto
{
    public int ClubId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Location { get; set; }
}
