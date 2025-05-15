using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackendW2Proj.Data;
using BackendW2Proj.Models;
using System.Linq;

namespace FullstackApp.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    [Authorize] // Require authentication for all endpoints
    public class TaskController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TaskController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")] // Only allow Admin users to access this endpoint
        public IActionResult GetAllTasks()
        {
            var tasks = _context.Tasks.ToList(); // Fetch all tasks from the database
            return Ok(tasks);
        }

        [HttpGet]
        public IActionResult GetTasks()
        {
            var userId = GetUserId();
            var tasks = _context.Tasks.Where(t => t.userId == userId).ToList(); // Fetch tasks for the logged-in user
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public IActionResult GetTaskById(int id)
        {
            var userId = GetUserId();
            var task = _context.Tasks.FirstOrDefault(t => t.id == id && t.userId == userId); // Fetch task by ID and user ID
            if (task == null)
            {
                return NotFound(new { Message = $"Task with ID {id} not found." });
            }
            return Ok(task);
        }

        [HttpPost]
        public IActionResult AddTask([FromBody] TaskModel newTask)
        {
            var userId = GetUserId();
            newTask.userId = userId; // Associate task with the logged-in user
            newTask.createdAt = DateTime.UtcNow; // Set the creation timestamp

            _context.Tasks.Add(newTask); // Add the task to the database
            _context.SaveChanges(); // Save changes to the database

            return Ok(newTask);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateTask(int id, [FromBody] TaskModel updatedTask)
        {
            var userId = GetUserId();
            var task = _context.Tasks.FirstOrDefault(t => t.id == id && t.userId == userId); // Fetch task by ID and user ID
            if (task == null)
            {
                return NotFound(new { Message = $"Task with ID {id} not found." });
            }

            // Update the task properties
            task.title = updatedTask.title;
            task.description = updatedTask.description;
            task.isCompleted = updatedTask.isCompleted;
            task.priority = updatedTask.priority;
            task.plannedCompletionDate = updatedTask.plannedCompletionDate;

            _context.SaveChanges(); // Save changes to the database
            return Ok(task);
        }

        [HttpDelete("{id}")]
        public IActionResult RemoveTask(int id)
        {
            // Log all claims for debugging
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
            }

            var userId = GetUserId();
            var isAdmin = User.Claims.Any(c =>
                (c.Type == "role" || c.Type == "roles" ||
                 c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role") &&
                c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase));

            TaskModel? task = null;
            if (isAdmin)
            {
                task = _context.Tasks.FirstOrDefault(t => t.id == id);
                if (task == null)
                {
                    return NotFound(new { Message = $"[Admin] Task with ID {id} not found." });
                }
            }
            else
            {
                task = _context.Tasks.FirstOrDefault(t => t.id == id && t.userId == userId);
                if (task == null)
                {
                    return NotFound(new { Message = $"[User] Task with ID {id} not found for your account." });
                }
            }

            // Remove all subtasks related to this task (optional, for clarity)
            var subtasks = _context.SubTasks.Where(st => st.taskId == id).ToList();
            _context.SubTasks.RemoveRange(subtasks);

            _context.Tasks.Remove(task);
            _context.SaveChanges();

            return Ok(new { Message = $"Task with ID {id} and its subtasks have been removed." });
        }



        private int GetUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        [HttpGet("{taskId}/subtasks")]
        public IActionResult GetSubTasksForTask(int taskId)
        {
            var userId = GetUserId();
            // Ensure the user owns the task (or is admin, if you want to allow that)
            var task = _context.Tasks.FirstOrDefault(t => t.id == taskId && t.userId == userId);
            if (task == null)
                return NotFound(new { Message = $"Task with ID {taskId} not found." });

            var subtasks = _context.SubTasks.Where(st => st.taskId == taskId).ToList();
            return Ok(subtasks);
        }



        [HttpPost("{taskId}/subtasks")]
        public IActionResult AddSubTask(int taskId, [FromBody] SubTaskModel subTask)
        {
            var userId = GetUserId();
            var task = _context.Tasks.FirstOrDefault(t => t.id == taskId && t.userId == userId);
            if (task == null)
                return NotFound(new { Message = $"Task with ID {taskId} not found." });

            subTask.taskId = taskId;
            // subTask.status can be set by the client or you can set a default here
            if (string.IsNullOrEmpty(subTask.status))
                subTask.status = "Not Started"; // Example default

            _context.SubTasks.Add(subTask);
            _context.SaveChanges();

            return Ok(subTask);
        }


        [HttpPut("subtasks/{subTaskId}")]
        public IActionResult UpdateSubTask(int subTaskId, [FromBody] SubTaskModel updatedSubTask)
        {
            var userId = GetUserId();
            var subTask = _context.SubTasks
                .Join(_context.Tasks, s => s.taskId, t => t.id, (s, t) => new { s, t })
                .Where(st => st.s.id == subTaskId && st.t.userId == userId)
                .Select(st => st.s)
                .FirstOrDefault();

            if (subTask == null)
                return NotFound(new { Message = $"SubTask with ID {subTaskId} not found." });

            subTask.title = updatedSubTask.title;
            subTask.description = updatedSubTask.description;
            subTask.isCompleted = updatedSubTask.isCompleted;
            subTask.status = updatedSubTask.status; 

            _context.SaveChanges();
            return Ok(subTask);
        }



        [HttpDelete("subtasks/{subTaskId}")]
        public IActionResult DeleteSubTask(int subTaskId)
        {
            var userId = GetUserId();
            var subTask = _context.SubTasks
                .Join(_context.Tasks, s => s.taskId, t => t.id, (s, t) => new { s, t })
                .Where(st => st.s.id == subTaskId && st.t.userId == userId)
                .Select(st => st.s)
                .FirstOrDefault();

            if (subTask == null)
                return NotFound(new { Message = $"SubTask with ID {subTaskId} not found." });

            _context.SubTasks.Remove(subTask);
            _context.SaveChanges();
            return Ok(new { Message = $"SubTask with ID {subTaskId} has been removed." });
        }






    }
}

