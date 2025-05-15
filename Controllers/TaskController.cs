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

            _context.Tasks.Remove(task);
            _context.SaveChanges();

            return Ok(new { Message = $"Task with ID {id} has been removed." });
        }



        private int GetUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }
    }
}

