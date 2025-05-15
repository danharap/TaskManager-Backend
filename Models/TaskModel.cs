namespace BackendW2Proj.Models
{
    public class TaskModel
    {
        public int id { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public bool isCompleted { get; set; }
        public DateTime createdAt { get; set; }
        public string? priority { get; set; }
        public int userId { get; set; }
        public DateTime? plannedCompletionDate { get; set; } // User's planned completion date
    }


}
