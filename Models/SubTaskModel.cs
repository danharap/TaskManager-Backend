using System.Text.Json.Serialization;

namespace BackendW2Proj.Models
{
    public class SubTaskModel
    {
        public int id { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public bool isCompleted { get; set; }
        public int taskId { get; set; } // Foreign key to TaskModel

        public string status { get; set; }

        [JsonIgnore]
        public TaskModel? Task { get; set; } // Navigation property
    }
}
