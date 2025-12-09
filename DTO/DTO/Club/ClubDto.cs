namespace DTO.DTO.Club
{
    public class ClubDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime? EstablishedDate { get; set; }
        public string? ImageClubsUrl { get; set; }
        public decimal? MembershipFee { get; set; }
        public string? Status { get; set; }
    }

}
