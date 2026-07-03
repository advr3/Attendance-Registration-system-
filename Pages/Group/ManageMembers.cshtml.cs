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
    public class ManageMembersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager; 

        public ManageMembersModel(ApplicationDbContext context, UserManager<User> userManager)
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
        public ICollection<GroupMember> Members { get; set; }
        

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
            Members = await _context.GroupMembers
                                    .Where(gm => gm.GroupId == GroupId)
                                    .Include(gm => gm.User)
                                    .ToListAsync();

            var joinLink = await _context.Links
                .FirstOrDefaultAsync(l => l.GroupId == GroupId && l.LinkURL.StartsWith(Variables.JoinTokenPrefix));

            if (joinLink != null)
            {
                JoinQrCodeUrl = Url.PageLink("/JoinGroup", null, new { linkUrl = joinLink.LinkURL });
                JoinQrCodeImage = Methods.GenerateQrCodeImage(JoinQrCodeUrl);
            }

            return Page();
        }

        // ========== Member ========== 
        /*
        public async Task<IActionResult> OnPostAddMemberAsync(string userIdentifier)
        {
            if (string.IsNullOrEmpty(userIdentifier))
            {
                StatusMessage = "Error: User identifier cannot be empty.";
                return RedirectToPage(new { id = GroupId });
            }

            var group = await _context.Groups.FindAsync(GroupId);
            if (group == null)
            {
                StatusMessage = "Error: Group not found.";
                return RedirectToPage(new { id = GroupId });
            }

            var user = await _userManager.FindByEmailAsync(userIdentifier); //?? await _userManager.FindByNameAsync(userIdentifier);

            if (user == null)
            {
                StatusMessage = "Error: User not found.";
                return RedirectToPage(new { id = GroupId });
            }

            var isAlreadyMember = await _context.GroupMembers.AnyAsync(gm => gm.UserId == user.Id && gm.GroupId == GroupId);
            if (isAlreadyMember)
            {
                StatusMessage = $"Error: {user.UserName} is already a member of this group.";
                return RedirectToPage(new { id = GroupId });
            }

            var newMember = new GroupMember
            {
                UserId = user.Id,
                GroupId = GroupId,
                joinDate = DateTime.UtcNow
            };

            _context.GroupMembers.Add(newMember);
            await _context.SaveChangesAsync();

            StatusMessage = $"Success: {user.UserName} has been added to the group.";
            return RedirectToPage(new { id = GroupId });
        }*/

        public async Task<JsonResult> OnPostAddMemberAsync([FromBody] AddMemberRequest request)
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

            var isAlreadyMember = await _context.GroupMembers.AnyAsync(gm => gm.UserId == user.Id && gm.GroupId == GroupId);
            if (isAlreadyMember)
            {
                return new JsonResult(new { success = false, message = $"{user.UserName} is already a member of this group." });
            }

            var newMember = new GroupMember
            {
                UserId = user.Id,
                GroupId = GroupId,
                joinDate = DateTime.UtcNow
            };

            _context.GroupMembers.Add(newMember);
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
        public class AddMemberRequest
        {
            public string userIdentifier { get; set; }
        }

        /*
        public async Task<IActionResult> OnPostRemoveMemberAsync(string userIdToRemove)
        {
            var memberToRemove = await _context.GroupMembers.FirstOrDefaultAsync(gm => gm.UserId == userIdToRemove && gm.GroupId == GroupId);
            if (memberToRemove == null)
            {
                StatusMessage = "Error: Member not found in this group.";
                return RedirectToPage(new { id = GroupId });
            }

            _context.GroupMembers.Remove(memberToRemove);
            await _context.SaveChangesAsync();
            
            StatusMessage = "Success: Member has been removed from the group.";
            return RedirectToPage(new { id = GroupId });
        }*/

        public async Task<JsonResult> OnPostRemoveMemberAsync([FromBody] RemoveMemberRequest request)
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

            var memberToRemove = await _context.GroupMembers.FirstOrDefaultAsync(gm => gm.UserId == request.userIdToRemove && gm.GroupId == GroupId);
            if (memberToRemove == null)
            {
                return new JsonResult(new { success = false, message = "Error: Member not found in this group." });
            }

            _context.GroupMembers.Remove(memberToRemove);
            await _context.SaveChangesAsync();

            // Return a success JSON response with the removed member's ID
            return new JsonResult(new
            {
                success = true,
                message = "Success: Member has been removed from the group.",
                userId = request.userIdToRemove
            });
        }

        // Data class to handle the incoming JSON request
        public class RemoveMemberRequest
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
                .FirstOrDefaultAsync(l => l.GroupId == GroupId && l.LinkURL.StartsWith(Variables.JoinTokenPrefix));

            if (oldJoinLink != null)
            {
                _context.Links.Remove(oldJoinLink);
            }

            var newLinkUrl = Variables.JoinTokenPrefix + Guid.NewGuid().ToString("N");
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
