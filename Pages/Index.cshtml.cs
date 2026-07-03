using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using personal_attendanse_system.Data;
using personal_attendanse_system.Data.Models;
using personal_attendanse_system.Globals;
using System.Linq;
using System.Security.Claims;

namespace personal_attendanse_system.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public bool IsLoggedIn { get; set; } = false;
        public bool IsAdmin { get; set; } = false;

        //public List<Data.Models.Group> Groups { get; set; } = new List<Data.Models.Group>();
        public List<Data.Models.Group> PublicGroups { get; set; } = new List<Data.Models.Group>();
        public List<Data.Models.Group> PrivateGroups { get; set; } = new List<Data.Models.Group>();
        public List<Data.Models.Group> GroupsWhereUserIsStaff { get; set; } = new List<Data.Models.Group>();

        public async Task OnGetAsync()
        {
            //IsLoggedIn = User.Identity.IsAuthenticated;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(userId == null)
            {
                IsLoggedIn = false;
                return;
            }
            else
            {
                IsLoggedIn = true;
                IsAdmin = await Methods.IsUserAdminAsync(_context, userId);// _dbContext.Admins.AnyAsync(a => a.UserId == currentUser.Id);

                if (IsAdmin)
                {
                    var groups = await _context.Groups.ToListAsync();

                    PublicGroups = groups.Where(g => g.isPublic == true).ToList();

                    PrivateGroups = groups.Where(g => g.isPublic == false).ToList();
                }
                else
                {
                    /*
                    // Get the IDs of groups where the user is a staff member.
                    GroupsWhereUserIsStaff = await _context.GroupStaffs
                        .Where(gm => gm.UserId == userId)
                        .Select(gm => gm.Group)
                        .ToListAsync();

                    var staffGroupIds = GroupsWhereUserIsStaff.Select(gs => gs.Id);


                    // Get private groups where the user is a member, but is not staff.
                    PrivateGroups = await _context.GroupMembers
                        .Where(gm => gm.UserId == userId && !staffGroupIds.Contains(gm.GroupId))
                        .Select(gm => gm.Group)
                        .ToListAsync();

                    var privateGroupIds = PrivateGroups.Select(gs => gs.Id);

                    // Get public groups, excluding the ones where the user is staff.
                    PublicGroups = await _context.Groups
                        .Where(g => g.isPublic && !staffGroupIds.Contains(g.Id) && !privateGroupIds.Contains(g.Id))
                        .ToListAsync();
                    */
                    var groups = await _context.Groups
                        .Include(g => g.GroupStaffs) // Eager load the staff members
                        .Where(g => g.GroupStaffs.Any(gs => gs.UserId == userId) || g.GroupMembers.Any(gm => gm.UserId == userId) || g.isPublic)
                        .ToListAsync();

                    GroupsWhereUserIsStaff = groups.Where(g => g.GroupStaffs.Any(gs => gs.UserId == userId)).ToList();//.Except(PublicGroups).Except(PrivateGroups).ToList();
                    
                    PublicGroups = groups.Where(g => g.isPublic == true).Except(GroupsWhereUserIsStaff).ToList();

                    PrivateGroups = groups.Where(g => g.isPublic == false).Except(GroupsWhereUserIsStaff).ToList();


                }
            }
        }
    }
}