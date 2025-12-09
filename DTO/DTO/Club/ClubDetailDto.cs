using DTO.DTO.Activity;

namespace DTO.DTO.Clubs;

public class ClubDetailDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string ImageUrl { get; set; }

    public string Status { get; set; }

    public List<ActivityDto> Activities { get; set; } = new();
}
