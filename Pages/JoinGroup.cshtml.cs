using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using personal_attendanse_system.Data.Models;
using personal_attendanse_system.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using personal_attendanse_system.Globals;

namespace personal_attendanse_system.Pages
{
    [Authorize]
    public class JoinGroupModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public JoinGroupModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string StatusMessage { get; set; }
        public Data.Models.Group Group { get; set; }

        public async Task<IActionResult> OnGetAsync(string linkUrl)
        {
            if (string.IsNullOrEmpty(linkUrl))
            {
                StatusMessage = "Error: Invalid link.";
                return Page();
            }

            var joinLink = await _context.Links
                .Include(l => l.Group)
                .FirstOrDefaultAsync(l => l.LinkURL == linkUrl);

            if (joinLink == null || DateTime.UtcNow > joinLink?.expirationDate)
            {
                StatusMessage = "Error: This join link is invalid or has expired.";
                return Page();
            }


            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                // If not logged in, they must log in first
                return RedirectToPage("/Account/Login", new { ReturnUrl = Url.PageLink("/JoinGroup", null, new { linkUrl }) });
            }

            if (joinLink.LinkURL.Contains(Variables.JoinTokenPrefix))
            {
                var isAlreadyMember = await _context.GroupMembers
                .AnyAsync(gm => gm.UserId == userId && gm.GroupId == joinLink.GroupId);

                if (isAlreadyMember)
                {
                    StatusMessage = "Success: You are already a member of this group.";
                    Group = joinLink.Group;
                    return Page();
                }

                var newMember = new GroupMember
                {
                    UserId = userId,
                    GroupId = joinLink.GroupId,
                    joinDate = DateTime.UtcNow
                };

                _context.GroupMembers.Add(newMember);
                await _context.SaveChangesAsync();
            }else if (joinLink.LinkURL.Contains(Variables.EmployTokenPrefix))
            {
                var isAlreadyStaff = await _context.GroupStaffs
                .AnyAsync(gm => gm.UserId == userId && gm.GroupId == joinLink.GroupId);

                if (isAlreadyStaff)
                {
                    StatusMessage = "Success: You are already a staff member of this group.";
                    Group = joinLink.Group;
                    return Page();
                }

                var newStaffMember = new GroupStaff
                {
                    UserId = userId,
                    GroupId = joinLink.GroupId,
                    joinDate = DateTime.UtcNow
                };

                _context.GroupStaffs.Add(newStaffMember);
                await _context.SaveChangesAsync();
            }
            else
            {
                Group = joinLink.Group;
                StatusMessage = "Error: There was a problem with your link.";
                return Page();
            }

            Group = joinLink.Group;
            StatusMessage = "Success: You have been added to the group!";

            return Page();
        }
    }
}
