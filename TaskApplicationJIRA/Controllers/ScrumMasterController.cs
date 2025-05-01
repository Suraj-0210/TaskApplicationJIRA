using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskApplicationJIRA.Data;
using TaskApplicationJIRA.Models.TaskModel;
using TaskApplicationJIRA.Models.TaskAssignment;
using TaskApplicationJIRA.Models.UserModel;
using TaskApplicationJIRA.ViewModels;

namespace TaskApplicationJIRA.Controllers
{
    [Authorize(Roles = "Scrum Master")]
    public class ScrumMasterController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScrumMasterController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var tasks = await _context.Tasks
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .ToListAsync();

            var developers = await _context.Users
                .Where(u => u.Role == "Developer")
                .ToListAsync();

            var assignments = await _context.TaskAssignments
                .Where(a => a.DeletedOn == null)
                .ToListAsync();

            var taskViewModels = tasks.Select(task =>
            {
                var assignment = assignments.FirstOrDefault(a => a.TaskId == task.Id);
                return new ScrumMasterTaskViewModel
                {
                    TaskId = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    CategoryName = task.Category?.Name,
                    PriorityLevel = task.Priority?.Level,
                    Status = task.Status,
                    DueDate = task.DueDate,
                    ImageUrl = task.ImageUrl,
                    AssignedToUserId = assignment?.AssignedToUserId
                };
            }).ToList();

            var viewModel = new ScrumMasterViewModel
            {
                Tasks = taskViewModels,
                Developers = developers
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Assign(int taskId, int developerId)
        {
            var existingAssignment = await _context.TaskAssignments
                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.DeletedOn == null);

            if (existingAssignment == null)
            {
                var assignment = new TaskAssignment
                {
                    TaskId = taskId,
                    AssignedToUserId = developerId,
                    CreatedBy = 1, // Get from session or user context
                    CreatedOn = DateTime.Now
                };
                _context.TaskAssignments.Add(assignment);
            }
            else
            {
                existingAssignment.AssignedToUserId = developerId;
                existingAssignment.UpdatedBy = 1;
                existingAssignment.UpdatedOn = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
