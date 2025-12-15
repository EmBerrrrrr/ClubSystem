using DTO.DTO;
using Service.Service.Interfaces;
using System.Collections.Concurrent;

namespace Service.Service.Implements
{
    public class InMemoryNotificationService : INotificationService
    {
        // key = AccountId
        private static readonly ConcurrentDictionary<int, List<NotificationDto>> _store
            = new();

        public void Push(int accountId, string title, string message)
        {
            var noti = new NotificationDto
            {
                AccountId = accountId,
                Title = title,
                Message = message
            };

            var list = _store.GetOrAdd(accountId, _ => new List<NotificationDto>());
            lock (list)
            {
                list.Add(noti);
            }
        }

        public List<NotificationDto> GetUnread(int accountId)
        {
            if (!_store.TryGetValue(accountId, out var list))
                return new List<NotificationDto>();

            return list.Where(x => !x.IsRead)
                       .OrderByDescending(x => x.CreatedAt)
                       .ToList();
        }

        public void MarkAsRead(int accountId, Guid notificationId)
        {
            if (!_store.TryGetValue(accountId, out var list))
                return;

            var noti = list.FirstOrDefault(x => x.Id == notificationId);
            if (noti != null)
                noti.IsRead = true;
        }
    }
}
