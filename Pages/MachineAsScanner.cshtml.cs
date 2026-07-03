using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using personal_attendanse_system.Data;
using personal_attendanse_system.Data.Models;
using personal_attendanse_system.Globals;
using System.Reflection.PortableExecutable;
using System.Security.Claims;

namespace personal_attendanse_system.Pages
{
    public class MachineAsScannerModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MachineAsScannerModel(ApplicationDbContext context)
        {
            _context = context;
        }

        
        public void OnGet()
        {
            // This is the initial page load, no logic is needed here
        }

        public async Task<JsonResult> OnPostCheckMachine([FromBody] CheckMachineRequest request)
        {
            if (request.MachineId == 0 || string.IsNullOrEmpty(request.MachineToken))
            {
                return new JsonResult(new { success = false, message = "Invalid request data." });
            }

            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.Id == request.MachineId && m.Token == request.MachineToken);
            if (machine == null)
            {
                return new JsonResult(new { success = false, message = "Invalid scanner configuration. Access denied." });
            }

            return new JsonResult(new 
            { 
                success = true,
                message = $"Success: Scanner successfully configured.",
                machine.Name
            });
        }
        public class CheckMachineRequest
        {
            public long MachineId { get; set; }
            public string MachineToken { get; set; }
        }

        public async Task<JsonResult> OnPostScanAsync([FromBody] ScanRequest request)
        {
            var now = DateTime.UtcNow;

            // 1. Validate input
            if (string.IsNullOrEmpty(request.UserCode) || request.MachineId == 0 || string.IsNullOrEmpty(request.MachineToken))
            {
                return new JsonResult(new { success = false, message = "Invalid request data." });
            }

            request.UserCode = request.UserCode.Split("token=")[1];

            // 2. Find the user by their unique QR code token
            var token = await _context.Tokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.QrCodeData == request.UserCode);

            if (token == null || token.User == null)
            {
                return new JsonResult(new { success = false, message = "Invalid QR code. User not found." });
            }

            // 3. Authenticate the machine and load its groups
            var machine = await _context.Machines
                .Include(m => m.MachineGroup)
                .ThenInclude(mg => mg.Group)
                .FirstOrDefaultAsync(m => m.Id == request.MachineId && m.Token == request.MachineToken);

            if (machine == null)
            {
                return new JsonResult(new { success = false, message = "Invalid scanner configuration. Access denied." });
            }

            // 4. Find the machine's assigned groups
            var machineGroupIds = new HashSet<long>(machine.MachineGroup.Select(mg => mg.GroupId));

            // 5. Find the user's groups
            var userGroupIds = new HashSet<long>(await _context.GroupMembers
                .Where(gm => gm.UserId == token.User.Id)
                .Select(gm => gm.GroupId)
                .ToListAsync());

            // 6. Find all public groups associated with the machine
            var publicGroupIds = new HashSet<long>(await _context.Groups
                .Where(g => g.isPublic && machineGroupIds.Contains(g.Id))
                .Select(g => g.Id)
                .ToListAsync());

            // 7. Combine the user's groups and the public groups to form the final set of valid groups
            var validGroupIds = userGroupIds.Union(publicGroupIds);

            if (!validGroupIds.Any())
            {
                return new JsonResult(new { success = false, message = "User is not a member of any valid group for this scanner." });
            }

            // 8. Get all active links for the valid groups
            var activeLinks = await _context.Links
                .Where(l => validGroupIds.Contains(l.GroupId) &&
                            l.LinkURL.StartsWith(Variables.AttendTokenPrefix) &&
                            l.activetionDate < now &&
                            l.expirationDate > now)
                .ToListAsync();

            if (!activeLinks.Any())
            {
                return new JsonResult(new { success = false, message = "No active group link to attend." });
            }

            var attendanceRecorded = false;
            var linkCanAddAttandance = false;
            foreach (var link in activeLinks)
            {
                // Check if the user has already attended this specific link session
                var alreadyAttended = await _context.Attendees
                    .AnyAsync(a => a.UserId == token.User.Id && a.LinkId == link.Id);

                if (!alreadyAttended)
                {
                    if (!link.canAttendThroughMachineScan)
                    {
                        continue;
                    }
                    // Register attendance for this link
                    var newAttendee = new Attendee
                    {
                        UserId = token.User.Id,
                        GroupId = link.GroupId,
                        LinkId = link.Id,
                        attendensDate = now
                    };

                    _context.Attendees.Add(newAttendee);
                    attendanceRecorded = true;
                    linkCanAddAttandance = true;
                }
            }

            if (!linkCanAddAttandance)
            {
                return new JsonResult(new { success = false, message = "All available sessions can not be attended through the attandance machine, try asking a staff member or scan it by yourself." });
            }

            if (!attendanceRecorded)
            {
                return new JsonResult(new { success = false, message = "User has already attended all available sessions." });
            }

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = $"Attendance recorded for {token.User.UserName}." });
        }

        public class ScanRequest
        {
            public string UserCode { get; set; }
            public long MachineId { get; set; }
            public string MachineToken { get; set; }
        }

    }
}