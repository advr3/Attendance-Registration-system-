using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using personal_attendanse_system.Data.Models;
using personal_attendanse_system.Data;
using QRCoder;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using personal_attendanse_system.Globals;

namespace personal_attendanse_system.Pages.Group
{
    [Authorize]
    public class ManageStaffsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ManageStaffsModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public long GroupId { get; set; }


        [TempData]
        public string StatusMessage { get; set; }

        // Properties for QR Code
        public string JoinQrCodeUrl { get; set; }
        public string JoinQrCodeImage { get; set; }
        public Data.Models.Group Group { get; set; }
        public ICollection<GroupStaff> Staffs { get; set; }

        

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = await _context.Admins.AnyAsync(a => a.UserId == userId);
            if (!isAdmin)
            {
                return Forbid();
            }
            Group = await _context.Groups.FindAsync(GroupId);

            if (Group == null)
            {
                return NotFound();
            }

            Staffs = await _context.GroupStaffs
                                    .Where(gm => gm.GroupId == GroupId)
                                    .Include(gm => gm.User)
                                    .ToListAsync();
            var joinLink = await _context.Links
                .FirstOrDefaultAsync(l => l.GroupId == GroupId && l.LinkURL.StartsWith(Variables.EmployTokenPrefix));

            if (joinLink != null)
            {
                JoinQrCodeUrl = Url.PageLink("/JoinGroup", null, new { linkUrl = joinLink.LinkURL });
                JoinQrCodeImage = Methods.GenerateQrCodeImage(JoinQrCodeUrl);
            }

            return Page();
        }

        public async Task<JsonResult> OnPostAddStaffAsync([FromBody] AddStaffRequest request)
        {
            if (string.IsNullOrEmpty(request.userIdentifier))
            {
                return new JsonResult(new { success = false, message = "User identifier cannot be empty." });
            }

            var group = await _context.Groups.FindAsync(GroupId);
            if (group == null)
            {
                return new JsonResult(new { success = false, message = "Group not found." });
            }

            var user = await _userManager.FindByEmailAsync(request.userIdentifier);// ?? await _userManager.FindByNameAsync(request.userIdentifier);

            if (user == null)
            {
                return new JsonResult(new { success = false, message = "User not found." });
            }

            var isAlreadyMember = await _context.GroupStaffs.AnyAsync(gm => gm.UserId == user.Id && gm.GroupId == GroupId);
            if (isAlreadyMember)
            {
                return new JsonResult(new { success = false, message = $"{user.UserName} is already a staff of this group." });
            }

            var newStaff = new GroupStaff
            {
                UserId = user.Id,
                GroupId = GroupId,
                joinDate = DateTime.UtcNow
            };

            _context.GroupStaffs.Add(newStaff);
            await _context.SaveChangesAsync();

            // Return a success JSON response with the new member data
            return new JsonResult(new
            {
                success = true,
                message = $"{user.UserName} has been added to the group.",
                member = new
                {
                    user.Id,
                    user.UserName,
                    user.Email
                }
            });
        }

        // Data class to handle the incoming JSON request
        public class AddStaffRequest
        {
            public string userIdentifier { get; set; }
        }

        public async Task<JsonResult> OnPostUpdateStaffPermissionsAsync([FromBody] UpdateStaffPermissionsRequest request)
        {
            var senderUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var isAdmin = await _context.Admins.AnyAsync(a => a.UserId == senderUserId);

            if (!isAdmin)
            {
                return new JsonResult(new { success = false, message = "Error: You do not have permission to perform this action." });
            }

            var groupStaff = await _context.GroupStaffs.FirstOrDefaultAsync(gs => gs.GroupId == request.GroupId && gs.UserId == request.UserId);

            if (groupStaff == null)
            {
                return new JsonResult(new { success = false, message = "Error: Staff member not found in this group." });
            }

            // Update the permissions
            groupStaff.canCreateScheduledLink = request.canCreateScheduledLink;
            groupStaff.canUpdateScheduledLink = request.canUpdateScheduledLink;
            groupStaff.canDeleteScheduledLink = request.canDeleteScheduledLink;
            groupStaff.canCreateLink = request.canCreateLink;
            groupStaff.canUpdateLink = request.canUpdateLink;
            groupStaff.canDeleteLink = request.canDeleteLink;

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Success: Staff permissions have been updated." });
        }

        // Data class to handle the incoming JSON request
        public class UpdateStaffPermissionsRequest
        {
            public long GroupId { get; set; }
            public string UserId { get; set; }
            public bool canCreateScheduledLink { get; set; }
            public bool canUpdateScheduledLink { get; set; }
            public bool canDeleteScheduledLink { get; set; }
            public bool canCreateLink { get; set; }
            public bool canUpdateLink { get; set; }
            public bool canDeleteLink { get; set; }
        }

        public async Task<JsonResult> OnPostRemoveStaffAsync([FromBody] RemoveStaffRequest request)
        {
            var group = await _context.Groups.FindAsync(GroupId);
            if (group == null)
            {
                return new JsonResult(new { success = false, message = "Group not found." });
            }

            var user = await _userManager.FindByIdAsync(request.userIdToRemove);// ?? await _userManager.FindByNameAsync(request.userIdToRemove);
            if (user == null)
            {
                return new JsonResult(new { success = false, message = "User not found." });
            }

            var memberToRemove = await _context.GroupStaffs.FirstOrDefaultAsync(gm => gm.UserId == request.userIdToRemove && gm.GroupId == GroupId);
            if (memberToRemove == null)
            {
                return new JsonResult(new { success = false, message = "Error: Staff not found in this group." });
            }

            _context.GroupStaffs.Remove(memberToRemove);
            await _context.SaveChangesAsync();

            // Return a success JSON response with the removed member's ID
            return new JsonResult(new
            {
                success = true,
                message = "Success: Staff has been removed from the group.",
                userId = request.userIdToRemove
            });
        }

        // Data class to handle the incoming JSON request
        public class RemoveStaffRequest
        {
            public string userIdToRemove { get; set; }
        }

        public async Task<IActionResult> OnPostGenerateJoinQrCodeAsync()
        {
            var group = await _context.Groups.FindAsync(GroupId);
            if (group == null)
            {
                StatusMessage = "Error: Group not found.";
                return RedirectToPage(new { id = GroupId });
            }
            var oldJoinLink = await _context.Links
                .FirstOrDefaultAsync(l => l.GroupId == GroupId && l.LinkURL.StartsWith(Variables.EmployTokenPrefix));

            if (oldJoinLink != null)
            {
                _context.Links.Remove(oldJoinLink);
            }

            var newLinkUrl = Variables.EmployTokenPrefix + Guid.NewGuid().ToString("N");
            var newJoinLink = new Link
            {
                GroupId = GroupId,
                LinkURL = newLinkUrl,
                activetionDate = DateTime.UtcNow,
                expirationDate = DateTime.UtcNow.AddHours(24)
            };

            _context.Links.Add(newJoinLink);
            await _context.SaveChangesAsync();

            StatusMessage = "Success: A new join QR code has been generated.";
            return RedirectToPage(new { id = GroupId });
        }
    }
}
