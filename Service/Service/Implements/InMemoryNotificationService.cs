using DTO.DTO;
using Service.Service.Interfaces;
using System.Collections.Concurrent;

namespace Service.Service.Implements
{
    /// <summary>
    /// Service lưu và quản lý notification in-memory (không lưu DB, chỉ tạm thời).
    /// 
    /// Công dụng: Push noti cho user (title + message), get unread, mark read.
    /// 
    /// Luồng: Không có API trực tiếp (internal use). Các service khác gọi Push khi có event (approve request, payment success...).
    ///    → Lưu vào ConcurrentDictionary (key=AccountId, value=List<NotificationDto>).
    ///    → Front-end polling GET /api/notifications/unread → GetUnread → Lọc !IsRead.
    /// 
    /// Tương tác giữa các API:
    /// - Được gọi từ service như ClubLeaderRequestService.Approve/Reject → Push noti cho user/admin.
    /// - Hoặc PayOS webhook success → Push noti thanh toán thành công.
    /// - User đọc noti → Gọi API mark-read → MarkAsRead (set IsRead=true).
    /// </summary>
    public class InMemoryNotificationService : INotificationService
    {
        // key = AccountId
        private static readonly ConcurrentDictionary<int, List<NotificationDto>> _store
            = new();

        /// <summary>
        /// Push noti mới cho user.
        /// 
        /// Luồng: Service gọi → Add vào list của accountId (CreatedAt auto = now).
        /// </summary>
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

        /// <summary>
        /// Lấy danh sách noti chưa đọc của user.
        /// 
        /// API: GET /api/notifications/unread
        /// Luồng: Lấy list của accountId → Lọc !IsRead → Sort descending CreatedAt.
        /// </summary>
        public List<NotificationDto> GetUnread(int accountId)
        {
            if (!_store.TryGetValue(accountId, out var list))
                return new List<NotificationDto>();

            return list.Where(x => !x.IsRead)
                       .OrderByDescending(x => x.CreatedAt)
                       .ToList();
        }

        /// <summary>
        /// Mark noti là đã đọc.
        /// 
        /// API: PUT /api/notifications/{notificationId}/read
        /// Luồng: Tìm noti bằng Id → Set IsRead=true (không xóa).
        /// </summary>
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
