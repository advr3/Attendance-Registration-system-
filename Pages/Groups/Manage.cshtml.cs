using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using personal_attendanse_system.Data;
using personal_attendanse_system.Data.Models;
using personal_attendanse_system.Globals;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static personal_attendanse_system.Pages.Group.IndexModel;

namespace personal_attendanse_system.Pages.Groups
{
    [Authorize]
    public class ManageModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public ManageModel(
            UserManager<User> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // ========== BindProperty ========== 
        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        // ========== TempData ========== 
        [TempData]
        public string StatusMessage { get; set; } = string.Empty;


        private bool IsAdmin { get; set; } = false;

        public List<Data.Models.Group> Groups { get; set; } = new List<Data.Models.Group>();


        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            IsAdmin = await Methods.IsUserAdminAsync(_context, userId);
            if (!IsAdmin)
            {
                StatusMessage = "Error: You do not have administrative permissions to view this page.";
                return Forbid();
            }

            var groupsQuery = _context.Groups.AsQueryable();

            if (!string.IsNullOrEmpty(SearchQuery))
            {
                groupsQuery = groupsQuery.Where(g => g.name.Contains(SearchQuery));
            }

            Groups = await groupsQuery.OrderBy(g => g.name).ToListAsync();

            return Page();
        }

        // ========== Group  ========== 
        
        public async Task<JsonResult> OnPostCreateAsync([FromBody] GroupCreateRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new JsonResult(new { success = false, message = "Error: User not authenticated." });
            }

            IsAdmin = await Methods.IsUserAdminAsync(_context, userId);
            if (!IsAdmin)
            {
                return new JsonResult(new { success = false, message = "Error: You do not have the authority to create this group." });
            }

            if (request == null) // Check the request object first!
            {
                return new JsonResult(new { success = false, message = "Error: Invalid or empty request data." });
            }

            var daysOfWeek = string.Join(",", request.days);
            var localNow = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local);
            var nextStartTimeUtc = TimeZoneInfo.ConvertTimeToUtc(localNow.Date.Add(request.startTime), TimeZoneInfo.Local);
            var nextEndTimeUtc = TimeZoneInfo.ConvertTimeToUtc(localNow.Date.Add(request.endTime), TimeZoneInfo.Local);

            var newGroup = new Data.Models.Group
            {
                name = request.Name,
                description = request.Description,
                isPublic = request.IsPublic,
                limitLinkActiveTime = request.LimitLinkActiveTime,
                creationDate = DateTime.UtcNow,
                DaysOfWeek = daysOfWeek,
                StartTime = nextStartTimeUtc,
                EndTime = nextEndTimeUtc

            };

            _context.Groups.Add(newGroup);
            await _context.SaveChangesAsync();

            // Return a success JSON response with the new group's data
            return new JsonResult(new
            {
                success = true,
                message = $"Group '{newGroup.name}' created successfully!",
                group = new
                {
                    newGroup.Id,
                    newGroup.name,
                    newGroup.isPublic,
                    creationDate = newGroup.creationDate.ToLocalTime().ToShortDateString()
                }
            });
        }

        public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
        {
            public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string? timeString = reader.GetString();

                // 1. Check for null or empty string and return the default value (null for TimeSpan?)
                if (string.IsNullOrEmpty(timeString))
                {
                    // Because your property is nullable (TimeSpan?), returning default(TimeSpan) 
                    // will effectively set it to null in the GroupCreateRequest object.
                    return default(TimeSpan);
                }

                // 2. Attempt to parse
                if (TimeSpan.TryParse(timeString, out TimeSpan result))
                {
                    return result;
                }

                // If it's not null/empty but still unparseable, then throw the error.
                throw new JsonException($"Unable to parse '{timeString}' to a TimeSpan.");
            }

            public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString("hh\\:mm"));
            }
        }

        public class GroupCreateRequest
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public bool IsPublic { get; set; }
            public bool LimitLinkActiveTime { get; set; }

            [JsonPropertyName("days")]
            public string[] days { get; set; } // Matches the lowercase "days" in JSON
            [JsonConverter(typeof(TimeSpanJsonConverter))]
            public TimeSpan startTime { get; set; }
            [JsonConverter(typeof(TimeSpanJsonConverter))]
            public TimeSpan endTime { get; set; }
        }

        public async Task<JsonResult> OnPostEditGroupAsync([FromBody] GroupEditRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new JsonResult(new { success = false, message = "Error: User not authenticated." });
            }

            IsAdmin = await Methods.IsUserAdminAsync(_context, userId);
            if (!IsAdmin)
            {
                return new JsonResult(new { success = false, message = "Error: You do not have the authority to edit this group." });
            }

            var groupToUpdate = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id.ToString() == request.Id);

            if (groupToUpdate == null)
            {
                return new JsonResult(new { success = false, message = "Error: Group not found for update." });
            }

            groupToUpdate.name = request.Name;
            groupToUpdate.isPublic = request.IsPublic;

            try
            {
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, message = "Group updated successfully!" });
            }
            catch (DbUpdateConcurrencyException)
            {
                return new JsonResult(new { success = false, message = "Error: The group you were trying to edit was modified by another user. Please try again." });
            }
            catch (Exception)
            {
                return new JsonResult(new { success = false, message = "Error: An unexpected error occurred during group update." });
            }
        }

        // Data class to handle the incoming JSON request
        public class GroupEditRequest
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public bool IsPublic { get; set; }
        }
        
        public async Task<JsonResult> OnPostDeleteGroupAsync(string id)
        {
            // The OnGetAsync() call is removed as it's not needed for a JSON response.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new JsonResult(new { success = false, message = "Error: User not authenticated." });
            }

            IsAdmin = await Methods.IsUserAdminAsync(_context, userId);
            if (!IsAdmin)
            {
                return new JsonResult(new { success = false, message = "Error: You do not have the authority to delete this group." });
            }

            var groupToDelete = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id.ToString() == id);

            if (groupToDelete == null)
            {
                return new JsonResult(new { success = false, message = "Error: Group not found for deletion." });
            }

            // 1. Find all associated RecurringTasks for this group
            var recurringTasks = await _context.RecurringTasks
                .Where(t => t.GroupId.ToString() == id)
                .ToListAsync();

            // 2. Remove the Hangfire jobs for these tasks
            var recurringJobManager = new RecurringJobManager();
            foreach (var task in recurringTasks)
            {
                recurringJobManager.RemoveIfExists($"task-{task.Id}");
            }

            _context.Groups.Remove(groupToDelete);
            await _context.SaveChangesAsync();

            // Return a success JSON response
            return new JsonResult(new
            {
                success = true,
                message = $"Group '{groupToDelete.name}' deleted successfully!",
                deletedGroupId = groupToDelete.Id // Return the ID for client-side removal
            });
        }
    }
}
