namespace DTO;

public class ClubDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageClubsUrl { get; set; }
    public decimal? MembershipFee { get; set; }
    public string Status { get; set; } = null!;
}
