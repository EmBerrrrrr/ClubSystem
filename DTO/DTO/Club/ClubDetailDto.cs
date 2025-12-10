using DTO.DTO.Activity;

namespace DTO.DTO.Clubs;

public class ClubDetailDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public List<ActivityDto> Activities { get; set; } = new();
}
