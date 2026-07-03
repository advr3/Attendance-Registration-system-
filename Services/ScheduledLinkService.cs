using Hangfire;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;
using personal_attendanse_system.Data;
using personal_attendanse_system.Data.Models;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using personal_attendanse_system.Globals;

namespace personal_attendanse_system.Services
{
    public class ScheduledLinkService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRecurringJobManager _recurringJobManager;


        public ScheduledLinkService(ApplicationDbContext context, IRecurringJobManager recurringJobManager)
        {
            _context = context;
            _recurringJobManager = recurringJobManager;
        }

        public async Task CreateAttendanceLink(long taskId)
        {
            var task = await _context.RecurringTasks
                                     .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return;

            var newLink = new Link
            {
                GroupId = task.GroupId,
                Name = task.Name,
                LinkURL = Variables.AttendTokenPrefix + Guid.NewGuid().ToString("N"),
                activetionDate = DateTime.UtcNow,
                expirationDate = task.EndTime,
                canAttendThroughMachineScan = task.canAttendThroughMachineScan,
                canAttendThroughStaffScan = task.canAttendThroughStaffScan,
                canAttendThroughUserScan = task.canAttendThroughUserScan,
            };

            _context.Links.Add(newLink);
            await _context.SaveChangesAsync();
        }

        public void CleanupOrphanedJobs()
        {
            // Get all recurring tasks from your application database
            var appTaskIds = _context.RecurringTasks
                                     .Select(t => t.Id)
                                     .ToList();

            // Get all recurring jobs from the Hangfire database
            var hangfireJobs = JobStorage.Current.GetConnection().GetRecurringJobs();

            foreach (var job in hangfireJobs)
            {
                // The job ID is in the format "task-123"
                if (job.Id.StartsWith("task-"))
                {
                    // Parse the taskId from the job ID string
                    if (int.TryParse(job.Id.Substring(5), out int taskId))
                    {
                        // If the taskId from Hangfire does not exist in your app's database, remove the job
                        if (!appTaskIds.Contains(taskId))
                        {
                            _recurringJobManager.RemoveIfExists(job.Id);
                        }
                    }
                }
            }
        }
    }
}
