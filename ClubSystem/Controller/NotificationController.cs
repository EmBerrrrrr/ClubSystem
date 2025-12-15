using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helper;
using Service.Service.Interfaces;

namespace ClubSystem.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _noti;

        public NotificationController(INotificationService noti)
        {
            _noti = noti;
        }

        // GET: api/notification
        [HttpGet]
        public IActionResult GetMyNotifications()
        {
            int accountId = User.GetAccountId(); 

            var data = _noti.GetUnread(accountId);
            return Ok(data);
        }

        // POST: api/notification/read/{id}
        [HttpPost("read/{id}")]
        public IActionResult MarkAsRead(Guid id)
        {
            int accountId = User.GetAccountId(); 

            _noti.MarkAsRead(accountId, id);
            return Ok();
        }
    }
}
