using DTO.DTO;

namespace Service.Service.Interfaces
{
    public interface INotificationService
    {
        void Push(int accountId, string title, string message);
        List<NotificationDto> GetUnread(int accountId);
        void MarkAsRead(int accountId, Guid notificationId);
    }
}
