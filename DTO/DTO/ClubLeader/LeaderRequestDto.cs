public class LeaderRequestDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string Status { get; set; }
    public DateTime RequestDate { get; set; }
    public string? Note { get; set; }
}
