namespace DTO.DTO.Activity
{
    public class ActivityParticipantDto
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public string ActivityTitle { get; set; }
        public int ClubId { get; set; }
        public string ClubName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Location { get; set; }
        public DateTime? RegisterTime { get; set; }
        public bool? Attended { get; set; }
        public string ActivityStatus { get; set; }
    }
}

