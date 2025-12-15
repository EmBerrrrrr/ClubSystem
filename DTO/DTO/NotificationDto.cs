namespace DTO.DTO
{
    public class NotificationDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int AccountId { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
    }
}
