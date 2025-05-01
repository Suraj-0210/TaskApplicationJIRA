using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskApplicationJIRA.Data;
using TaskApplicationJIRA.Models.TaskAssignment;
using TaskApplicationJIRA.Models.UserModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TaskApplicationJIRA.Models.ViewModels;

namespace TaskApplicationJIRA.Controllers
{
    [Authorize(Roles = "Developer")]
    public class DeveloperController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DeveloperController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get the logged-in user's email (or UserId)
            var currentUserEmail = User.FindFirstValue(ClaimTypes.Email);
            var currentUser = _context.Users.FirstOrDefault(u => u.Email == currentUserEmail);

            // Fetch all tasks assigned to the logged-in developer
            var assignedTasks = await _context.TaskAssignments
            .Where(ta => ta.AssignedToUserId == currentUser.UserId)
            .Include(ta => ta.Task)
            .ThenInclude(task => task.Category) // Include Category
            .Include(ta => ta.Task)
            .ThenInclude(task => task.Priority) // Include Priority
            .ToListAsync();


            var tasks = assignedTasks.Select(ta => new TaskAssignedDevView
            {
                TaskId = ta.Task.Id,
                Title = ta.Task.Title,
                Description = ta.Task.Description,
                CategoryName = ta.Task.Category?.Name,
                PriorityLevel = ta.Task.Priority?.Level,
                Status = ta.Task.Status,
                DueDate = ta.Task.DueDate,
                ImageUrl = ta.Task.ImageUrl
            }).ToList();

            return View(tasks);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int taskId, string status)
        {
            var taskAssignment = await _context.TaskAssignments
                .Include(ta => ta.Task)
                .FirstOrDefaultAsync(ta => ta.TaskId == taskId);

            if (taskAssignment != null)
            {
                taskAssignment.Task.Status = status;

                // Update modified date (optional)
                taskAssignment.Task.UpdatedOn = DateTime.Now;

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

    }
}
