using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackendW2Proj.Data;
using BackendW2Proj.Models;
using System.Linq;

namespace BackendW2Proj.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult CreateNotification([FromBody] NotificationModel notification)
        {
            // Get the userId from the JWT claims
            var userId = int.Parse(User.Claims.First(c => c.Type == "id").Value);

            // Set the userId and other server-side properties
            notification.userId = userId;
            notification.createdAt = DateTime.UtcNow;
            notification.isRead = false;

            _context.Notifications.Add(notification);
            _context.SaveChanges();

            return Ok(notification);
        }

        [HttpDelete("clear-all")]
        public IActionResult ClearAllNotifications()
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "id").Value);

            // Find all notifications for this user
            var notifications = _context.Notifications.Where(n => n.userId == userId).ToList();

            if (notifications.Count == 0)
                return Ok(new { Message = "No notifications to clear." });

            // Remove all notifications for this user
            _context.Notifications.RemoveRange(notifications);
            _context.SaveChanges();

            return Ok(new { Message = $"Cleared {notifications.Count} notifications." });
        }


        [HttpGet]
        public IActionResult GetNotifications()
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "id").Value);
            var notifications = _context.Notifications
                .Where(n => n.userId == userId)
                .OrderByDescending(n => n.createdAt)
                .ToList();
            return Ok(notifications);
        }

        [HttpPut("{id}/read")]
        public IActionResult MarkAsRead(int id)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "id").Value);
            var notification = _context.Notifications.FirstOrDefault(n => n.id == id && n.userId == userId);
            if (notification == null)
                return NotFound();

            notification.isRead = true;
            _context.SaveChanges();
            return Ok(notification);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteNotification(int id)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "id").Value);
            var notification = _context.Notifications.FirstOrDefault(n => n.id == id && n.userId == userId);
            if (notification == null)
                return NotFound();

            _context.Notifications.Remove(notification);
            _context.SaveChanges();
            return Ok(new { Message = $"Notification with ID {id} has been deleted." });
        }

    }
}
