using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using personal_attendanse_system.Data;
using personal_attendanse_system.Data.Models;
using personal_attendanse_system.Globals;
using personal_attendanse_system.Services;
using QRCoder;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace personal_attendanse_system.Pages.Group
{
    [Authorize]
    public class IndexModel : PageModel
    {
        // ========== Private Property ==========
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
            QRCodeCache = new Dictionary<long, string>();
        }

        // ========== BindProperty ========== 
        [BindProperty(SupportsGet = true)]
        public long Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Filter { get; set; } = "all";

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; } = "desc";

        // ========== TempData ==========
        [TempData]
        public string StatusMessage { get; set; }

        // ========== Public ==========
        public bool IsAdmin { get; set; } = false;
        public Data.Models.Group Group { get; set; }
        public GroupStaff IsStaff { get; set; }
        public IEnumerable<Link> Links { get; set; }
        public HashSet<long> AttendedLinkIds { get; set; }
        public Dictionary<long, string> QRCodeCache { get; set; }
        public List<RecurringTask> RecurringTasks { get; set; }



        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Single query to get the Group and its related data for in-memory checks.
            Group = await _context.Groups
                .Include(g => g.GroupStaffs)
                .Include(g => g.GroupMembers)
                .FirstOrDefaultAsync(g => g.Id == Id);

            if (Group == null)
            {
                return NotFound();
            }

            // In-memory checks.
            // NOTE: IsUserAdminAsync might still require a separate DB call depending on its implementation.
            IsAdmin = await Methods.IsUserAdminAsync(_context, userId);
            IsStaff = Group.GroupStaffs.FirstOrDefault(gs => gs.UserId == userId);
            var isGroupMember = Group.GroupMembers.Any(gm => gm.UserId == userId);

            if (!IsAdmin && !Group.isPublic && !isGroupMember && IsStaff == null)
            {
                StatusMessage = "Error: You are not a member of this group.";
                return Forbid();
            }

            // The rest of your logic, now with a single data source.
            if (IsAdmin || IsStaff != null)
            {
                var groupLinksAndTasks = await _context.Groups
                    .Where(g => g.Id == Id) // Filter to the specific Group
                    .Include(g => g.RecurringTasks) // Filter RecurringTasks by User
                    .Include(g => g.Links.Where(l => l.LinkURL.StartsWith(Variables.AttendTokenPrefix))) // Filter Links
                    .FirstOrDefaultAsync(g => g.Id == Id);


                Links = groupLinksAndTasks.Links.ToList();

                foreach (var link in Links)
                {
                    QRCodeCache[link.Id] = Methods.GenerateQRCodeAsBase64(Url.PageLink("/Attend", null, new { linkUrl = link.LinkURL }));
                }

                RecurringTasks = groupLinksAndTasks.RecurringTasks.ToList();
            }
            else
            {

                var allLinks = await _context.Links
                    // First, filter by GroupId and LinkURL to reduce the dataset.
                    .Where(l => l.GroupId == Id && l.LinkURL.StartsWith(Variables.AttendTokenPrefix))
                    // Eager load the Attendees for each link.
                    .Include(l => l.Attendees)
                    // Execute the query and materialize the data.
                    .ToListAsync();

                // Now, perform all filtering and sorting in memory.
                // This is fast because all the data is already loaded.

                if (Filter == "attended")
                {
                    Links = allLinks
                        .Where(l => l.Attendees != null && l.Attendees.Any(a => a.UserId == userId))
                        .ToList();
                }
                else if (Filter == "notAttended")
                {
                    Links = allLinks
                        .Where(l => l.Attendees == null || !l.Attendees.Any(a => a.UserId == userId))
                        .ToList();
                }
                else
                {
                    Links = allLinks;
                }

                // Apply the sorting after filtering
                Links = (SortOrder == "desc" ? Links.OrderByDescending(l => l.creationDate) : Links.OrderBy(l => l.creationDate)).ToList();
            }

            return Page();
        }


        // ========== Link ========== 
        public async Task<JsonResult> OnPostGenerateLinkAsync([FromBody] GenerateLinkRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new JsonResult(new { success = false, message = "User not logged in" });
            }

            var isStaffWithPermissions = await _context.GroupStaffs.AnyAsync(gs => gs.UserId == userId && gs.GroupId == Id && gs.canCreateLink);

            if (!isStaffWithPermissions)
            {
                var isAdmin = await Methods.IsUserAdminAsync(_context, userId);
                if (!isAdmin)
                {
                    return new JsonResult(new { success = false, message = "You do not have permissions to create links" });
                }
            }

            var newLink = new Link
            {
                GroupId = Id,
                Name = request.linkName,
                LinkURL = Variables.AttendTokenPrefix + Guid.NewGuid().ToString("N"),
                creationDate = DateTime.UtcNow,
                activetionDate = request.activationDate.ToUniversalTime(),
                expirationDate = request.expirationDate.ToUniversalTime(),

                canAttendThroughUserScan = request.canAttendThroughUserScan,
                canAttendThroughStaffScan = request.canAttendThroughStaffScan,
                canAttendThroughMachineScan = request.canAttendThroughMachineScan
            };

            _context.Links.Add(newLink);
            await _context.SaveChangesAsync();

            

            // Return the new link data to the client to update the UI
            return new JsonResult(new
            {
                success = true,
                message = "New attendance link generated successfully",
                link = new
                {
                    newLink.Id,
                    newLink.Name,
                    linkUrl = newLink.LinkURL,
                    newLink.creationDate,
                    activetionDate = newLink.activetionDate,//.ToLocalTime().ToString("g"),
                    expirationDate = newLink.expirationDate,//.ToLocalTime().ToString("g"),
                    qrCode = Methods.GenerateQRCodeAsBase64(Url.PageLink("/Attend", null, new { linkUrl = newLink.LinkURL })),
                    newLink.canAttendThroughUserScan,
                    newLink.canAttendThroughStaffScan,
                    newLink.canAttendThroughMachineScan
                }
            });
        }

        public class GenerateLinkRequest
        {
            public DateTime activationDate { get; set; }
            public DateTime expirationDate { get; set; }
            public string linkName { get; set; }

            public bool canAttendThroughUserScan { get; set; } = false;
            public bool canAttendThroughStaffScan { get; set; } = false;
            public bool canAttendThroughMachineScan { get; set; } = false;
        }

        public async Task<IActionResult> OnPostDeleteLinkAsync([FromBody] DeleteLinkRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new JsonResult(new { success = false, message = "User not authenticated" });
            }

            // Convert linkId to long
            if (!long.TryParse(request.linkId, out var linkId))
            {
                return new JsonResult(new { success = false, message = "Invalid link ID" });
            }

            var link = await _context.Links.FirstOrDefaultAsync(l => l.Id == linkId);
            if (link == null)
            {
                return new JsonResult(new { success = false, message = "Invalid link" });
            }

            // Authorization logic
            var isStaffWithPermissions = await _context.GroupStaffs.AnyAsync(gs => gs.UserId == userId && gs.GroupId == link.GroupId && gs.canDeleteLink);
            if (!isStaffWithPermissions)
            {
                var isAdmin = await Methods.IsUserAdminAsync(_context, userId);
                if (!isAdmin)
                {
                    return new JsonResult(new { success = false, message = "You do not have permissions to delete links" });
                }
            }

            _context.Links.Remove(link);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = $"{link.Name} Link has been removed" });
        }

        // Create a new class to receive the JSON body from the client
        public class DeleteLinkRequest
        {
            public string linkId { get; set; }
        }


        // ========== ScheduledLink ========== 
        public async Task<JsonResult> OnPostCreateScheduledLink([FromBody] CreateScheduledLinkRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new JsonResult(new { success = false, message = "User not logged in." });
            }

            var isStaffWithPermissions = await _context.GroupStaffs.AnyAsync(gs => gs.UserId == userId && gs.GroupId == Id && gs.canCreateScheduledLink);

            if (!isStaffWithPermissions)
            {
                var isAdmin = await Methods.IsUserAdminAsync(_context, userId);
                if (!isAdmin)
                {
                    return new JsonResult(new { success = false, message = "Error: You do not have permissions to create scheduled links." });
                }
            }

            var daysOfWeek = string.Join(",", request.days);
            var localNow = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Local);
            var nextStartTimeUtc = TimeZoneInfo.ConvertTimeToUtc(localNow.Date.Add(request.startTime), TimeZoneInfo.Local);
            var nextEndTimeUtc = TimeZoneInfo.ConvertTimeToUtc(localNow.Date.Add(request.endTime), TimeZoneInfo.Local);

            var newRecurringTask = new RecurringTask
            {
                UserId = userId,
                Name = request.name,
                DaysOfWeek = daysOfWeek,
                StartTime = nextStartTimeUtc,
                EndTime = nextEndTimeUtc,
                GroupId = Id,

                canAttendThroughUserScan = request.canAttendThroughUserScan,
                canAttendThroughStaffScan = request.canAttendThroughStaffScan,
                canAttendThroughMachineScan = request.canAttendThroughMachineScan
            };

            _context.RecurringTasks.Add(newRecurringTask);
            await _context.SaveChangesAsync();

            var cron = Methods.GenerateCronExpression(request.days, nextStartTimeUtc);
            RecurringJob.AddOrUpdate<ScheduledLinkService>(
                $"task-{newRecurringTask.Id}",
                service => service.CreateAttendanceLink(newRecurringTask.Id),
                cron
            );

            return new JsonResult(new
            {
                success = true,
                message = $"{newRecurringTask.Name} scheduled link has been created successfully.",
                task = new
                {
                    newRecurringTask.Id,
                    newRecurringTask.Name,
                    newRecurringTask.DaysOfWeek,
                    newRecurringTask.StartTime,
                    newRecurringTask.EndTime,

                    newRecurringTask.canAttendThroughUserScan,
                    newRecurringTask.canAttendThroughStaffScan,
                    newRecurringTask.canAttendThroughMachineScan
                }
            });
        }
        public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
        {
            public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string? timeString = reader.GetString();
                if (TimeSpan.TryParse(timeString, out TimeSpan result))
                {
                    return result;
                }
                throw new JsonException($"Unable to parse '{timeString}' to a TimeSpan.");
            }

            public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString("hh\\:mm"));
            }
        }

        public class CreateScheduledLinkRequest
        {
            public string name { get; set; }
            [JsonPropertyName("days")]
            public string[] days { get; set; } // Matches the lowercase "days" in JSON
            [JsonConverter(typeof(TimeSpanJsonConverter))]
            public TimeSpan startTime { get; set; }
            [JsonConverter(typeof(TimeSpanJsonConverter))]
            public TimeSpan endTime { get; set; }
            public bool canAttendThroughUserScan { get; set; } = false;
            public bool canAttendThroughStaffScan { get; set; } = false;
            public bool canAttendThroughMachineScan { get; set; } = false;
        }

        public async Task<JsonResult> OnPostDeleteScheduledLink([FromBody] DeleteScheduledLinkRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new JsonResult(new { success = false, message = "User not logged in." });
            }

            var isStaffWithPermissions = await _context.GroupStaffs.AnyAsync(gs => gs.UserId == userId && gs.GroupId == Id && gs.canDeleteScheduledLink);

            if (!isStaffWithPermissions)
            {
                var isAdmin = await Methods.IsUserAdminAsync(_context, userId);
                if (!isAdmin)
                {
                    return new JsonResult(new { success = false, message = "Error: You do not have permissions to delete scheduled links." });
                }
            }

            var task = await _context.RecurringTasks.FindAsync(request.id);
            if (task == null)
            {
                return new JsonResult(new { success = false, message = "Error: Scheduled task not found." });
            }

            RecurringJob.RemoveIfExists($"task-{task.Id}");
            _context.RecurringTasks.Remove(task);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = $"{task.Name} scheduled link has been deleted successfully." });
        }

        public class DeleteScheduledLinkRequest
        {
            public long id { get; set; }
        }


        // ========== Other ========== 
        public async Task<JsonResult> OnGetLiveAttendeesAsync(long id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new JsonResult(new List<object>());
            }

            // Check the most likely scenario first: is the user a staff member with link creation permissions?
            var isStaffWithPermissions = await _context.GroupStaffs.AnyAsync(gs => gs.UserId == userId && gs.GroupId == Id);

            // If not, check the less likely scenario: is the user an admin?
            if (!isStaffWithPermissions)
            {
                var isAdmin = await Methods.IsUserAdminAsync(_context, userId);
                if (!isAdmin)
                {
                    return new JsonResult(new List<object>());
                }
            }

            var now = DateTime.UtcNow;

            var activeLinks = await _context.Links
                .Where(l => l.GroupId == id && now >= l.activetionDate && now <= l.expirationDate)
                .Select(l => l.Id)
                .ToListAsync();

            if (!activeLinks.Any())
            {
                return new JsonResult(new List<object>());
            }

            var liveAttendees = await _context.Attendees
                .Include(a => a.User)
                .Where(a => a.GroupId == id && a.LinkId.HasValue && activeLinks.Contains(a.LinkId.Value))
                .OrderByDescending(a => a.attendensDate)
                .Select(a => new
                {
                    a.User.UserName,
                    AttendanceDate = a.attendensDate
                })
                .ToListAsync();

            return new JsonResult(liveAttendees);
        }

        public async Task<IActionResult> OnPostScanFromWeb([FromBody] ScanRequest request)
        {
            if (string.IsNullOrEmpty(request.LinkUrl))
            {
                return new JsonResult(new { success = false, message = "Invalid link URL." });
            }

            // Assuming you have a way to get the current user's ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return new JsonResult(new { success = false, message = "User not logged in." });
            }

            // Find the link by LinkURL
            var link = await _context.Links
                                     .Include(l => l.Group)
                                     .FirstOrDefaultAsync(l => l.LinkURL == request.LinkUrl);

            if (link == null)
            {
                return new JsonResult(new { success = false, message = "Attendance link not found." });
            }

            if (link.GroupId != request.GroupId)
            {
                return new JsonResult(new { success = false, message = "This attendance link is not for this group." });
            }

            // Check if the link is active
            if (link.activetionDate > DateTime.UtcNow || link.expirationDate < DateTime.UtcNow)
            {
                return new JsonResult(new { success = false, message = "This attendance link is not active." });
            }

            // Check if the user has already attended
            var attendance = await _context.Attendees
                                           .FirstOrDefaultAsync(a => a.LinkId == link.Id && a.UserId == userId);

            
            if (attendance != null)
            {
                TempData["AttendanceStatus"] = "You have already attended this session.";
                TempData["AttendanceSuccess"] = false;
                return new JsonResult(new { success = true, message = "You have already attended this session." }); // Return a minimal success flag
            }

            // Mark the user as attended
            var newAttendance = new Attendee
            {
                UserId = userId,
                GroupId = link.GroupId,
                LinkId = link.Id,
                attendensDate = DateTime.UtcNow
            };

            _context.Attendees.Add(newAttendance);
            await _context.SaveChangesAsync();

            // Find the user's name to provide a personalized success message
            var user = await _userManager.FindByIdAsync(userId);
            var userName = user?.UserName ?? "Unknown User";

            TempData["AttendanceStatus"] = $"{userName}, your attendance has been recorded successfully!";
            TempData["AttendanceSuccess"] = true;

            return new JsonResult(new { success = true, message = $"{userName} has been successfully registered!" });
        }




        // ========== Single Use Classes ========== 
        public class ScanRequest
        {
            public string LinkUrl { get; set; }
            public long GroupId { get; set; }
        }
    
    }
}
