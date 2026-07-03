using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using personal_attendanse_system.Data.Models;
using personal_attendanse_system.Data;
using System.Security.Claims;

namespace personal_attendanse_system.Pages
{
    public class AttendModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AttendModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(string linkUrl)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }


            var link = await _context.Links
                .FirstOrDefaultAsync(l => l.LinkURL == linkUrl);

            if (link == null || link.expirationDate < DateTime.UtcNow || !link.canAttendThroughUserScan)
            {
                if (!link.canAttendThroughUserScan)
                {
                    TempData["Message"] = "This attendance link can not be scaned by the user for use, try asking a staff member or use a an attendace machine.";
                }
                else
                {
                    TempData["Message"] = "This attendance link is expired or invalid.";
                }
                // Print the URL to the debug console
                var redirectUrl = Url.Page("/Group/Index", new { id = link.GroupId });
                Console.WriteLine($"Redirecting to URL: {redirectUrl}");
                return Redirect(redirectUrl);
            }

            /*
            var isGroupMember = await _context.GroupMembers.AnyAsync(gm => gm.UserId == userId && gm.GroupId == link.GroupId);
            var group = await _context.Groups.FindAsync(link.GroupId);

            if (!isGroupMember && !group.isPublic) 
            {
                TempData["Message"] = "You are not a member of this group.";
                return Forbid();
            }
            */

            var group = await _context.Groups.FindAsync(link.GroupId);
            if (!group.isPublic)
            {
                var isGroupMember = await _context.GroupMembers.AnyAsync(gm => gm.UserId == userId && gm.GroupId == link.GroupId);
                if (!isGroupMember)
                {
                    TempData["Message"] = "You are not a member of this group.";
                    return Forbid();
                }
            }

            var existingAttendee = await _context.Attendees
                .FirstOrDefaultAsync(a => a.UserId == userId && a.LinkId == link.Id);

            if (existingAttendee != null)
            {
                TempData["Message"] = "You have already registered attendance for this link.";
                // Print the URL to the debug console
                var redirectUrl = Url.Page("/Group/Index", new { id = link.GroupId });
                Console.WriteLine($"Redirecting to URL: {redirectUrl}");
                return Redirect(redirectUrl);
            }

            var newAttendee = new Attendee
            {
                UserId = userId,
                GroupId = link.GroupId,
                LinkId = link.Id,
                attendensDate = DateTime.UtcNow
            };

            _context.Attendees.Add(newAttendee);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Attendance registered successfully!";
            // Print the URL to the debug console
            var redirectUrlSuccess = Url.Page("/Group/Index", new { id = link.GroupId });
            Console.WriteLine($"Redirecting to URL: {redirectUrlSuccess}");
            return Redirect(redirectUrlSuccess);
        }
    }
}