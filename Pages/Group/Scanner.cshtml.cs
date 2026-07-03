using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using personal_attendanse_system.Data.Models;
using personal_attendanse_system.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using personal_attendanse_system.Globals;

namespace personal_attendanse_system.Pages.Group
{
    [Authorize]
    public class ScannerModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ScannerModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public string LinkUrl { get; set; }

        public Link Link { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Link = await _context.Links.FirstOrDefaultAsync(l => l.LinkURL == LinkUrl);
            if (Link == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = await _context.Admins.AnyAsync(a => a.UserId == userId);
            if (!isAdmin)
            {
                var isStaff = await Methods.IsUserStaffAsync(_context, userId, Link.GroupId.ToString());// False
                if (!isStaff)
                {
                    return Forbid();
                }
            }

            var now = DateTime.UtcNow;
            if (now < Link.activetionDate || now > Link.expirationDate)
            {
                // The link is not currently active for scanning
                return RedirectToPage("/Group", new { id = Link.GroupId });
            }

            return Page();
        }

        public async Task<JsonResult> OnPostScanAsync([FromBody] ScanRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Find the active link
            var now = DateTime.UtcNow;
            var activeLink = await _context.Links
                .FirstOrDefaultAsync(l => l.LinkURL == request.LinkUrl && now >= l.activetionDate && now <= l.expirationDate);

            if (activeLink == null)
            {
                return new JsonResult(new { success = false, message = "No active attendance session." });
            }

            var isAdmin = await _context.Admins.AnyAsync(a => a.UserId == userId);
            if (!isAdmin)
            {
                var isStaff = await Methods.IsUserStaffAsync(_context, userId, activeLink.GroupId.ToString());//await _context.GroupStaffs.FirstOrDefaultAsync(a => a.UserId == userId && a.GroupId == Id);// False
                if (!isStaff)
                {
                    return new JsonResult(new { success = false, message = "Unauthorized access." });
                }
            }

            if (!activeLink.canAttendThroughStaffScan)
            {
                return new JsonResult(new { success = false, message = "This attendance link is can not be scaned by staff members for use, try scanning it or use a an attendace machine." });
            }

            request.ScannedCode = request.ScannedCode.Split("token=")[1];

            // Find the user by their unique QR code token
            var token = await _context.Tokens
                .FirstOrDefaultAsync(t => t.QrCodeData == request.ScannedCode);

            if (token == null)
            {
                return new JsonResult(new { success = false, message = "Invalid QR code." });
            }

            var group = await _context.Groups.FindAsync(activeLink.GroupId);
            if (!group.isPublic)
            {
                var isGroupMember = await _context.GroupMembers.AnyAsync(gm => gm.UserId == token.UserId && gm.GroupId == group.Id);
                if (!isGroupMember)
                {
                    return new JsonResult(new { success = false, message = "User is not a member of this private group." });
                }
            }

            // Check if the user has already attended this session
            var alreadyAttended = await _context.Attendees
                .AnyAsync(a => a.UserId == token.UserId && a.LinkId == activeLink.Id);

            if (alreadyAttended)
            {
                return new JsonResult(new { success = false, message = "User has already attended this session." });
            }

            // Register attendance
            var newAttendee = new Attendee
            {
                UserId = token.UserId,
                GroupId = activeLink.GroupId,
                LinkId = activeLink.Id,
                attendensDate = now
            };

            _context.Attendees.Add(newAttendee);
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(token.UserId);

            return new JsonResult(new { success = true, message = $"Attendance registered for {user?.UserName ?? "N/A"}." });
        }
    }

    public class ScanRequest
    {
        public string ScannedCode { get; set; }
        public string LinkUrl { get; set; }
    }
}
