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
    public class AttendeesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AttendeesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public long LinkId { get; set; }

        public Data.Models.Group Group { get; set; }
        public Link Link { get; set; }
        public IEnumerable<Attendee> Attendees { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Single query to get the link and related group data.
            // Eager load the Group, GroupStaffs, and Users for a single, efficient database call.
            Link = await _context.Links
                .Include(l => l.Group)
                    .ThenInclude(g => g.GroupStaffs)
                        .ThenInclude(gs => gs.User)
                .FirstOrDefaultAsync(l => l.Id == LinkId);

            if (Link == null)
            {
                return NotFound();
            }

            // In-memory checks to avoid multiple database calls.
            // Check if the user is an admin by seeing if their ID exists in the GroupStaffs of the group.
            var isStaff = Link.Group.GroupStaffs.Any(gs => gs.UserId == userId);
            var isAdmin = await Methods.IsUserAdminAsync(_context, userId); // Admin check may still need a separate call depending on its implementation.

            if (!isAdmin && !isStaff)
            {
                return Forbid();
            }

            Group = Link.Group; // Assign the eager-loaded Group

            Attendees = await _context.Attendees
                .Include(a => a.User)
                .Where(a => a.LinkId == LinkId)
                .OrderByDescending(a => a.attendensDate)
                .ToListAsync();

            return Page();
        }
    }
}
