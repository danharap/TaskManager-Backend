using System;

namespace BackendW2Proj.Models
{
    public class NotificationModel
    {
        public int id { get; set; }
        public int userId { get; set; }
        public string type { get; set; } = string.Empty; 
        public string message { get; set; } = string.Empty;
        public DateTime createdAt { get; set; }
        public bool isRead { get; set; } = false;
    }
}
