using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using personal_attendanse_system.Data;
using personal_attendanse_system.Data.Models;
using System.ComponentModel.DataAnnotations;
using personal_attendanse_system.Globals;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

namespace personal_attendanse_system.Pages.Groups.Machine
{
    public class ManageModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public long Id { get; set; }

        public Data.Models.Machine Machine { get; set; }

        public List<Data.Models.Group> AllGroups { get; set; } = new List<Data.Models.Group>();

        public List<long> AssignedGroupIds { get; set; } = new List<long>();

        [TempData]
        public string StatusMessage { get; set; }

        public bool IsAdmin { get; set; }

        public string QrCodeImage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                IsAdmin = await _context.Admins.AnyAsync(a => a.UserId == userId);
            }

            if (!IsAdmin)
            {
                StatusMessage = "Error: You do not have permission to manage this machine.";
                return Forbid();
            }

            Machine = await _context.Machines.FirstOrDefaultAsync(m => m.Id == Id);

            if (Machine == null)
            {
                StatusMessage = "Error: Machine not found.";
                return RedirectToPage("/Groups/Machine");
            }


            var QrCodeData = $"MachineID_{Machine.Id}&MachineToken_{Machine.Token}";
            QrCodeImage = Methods.GenerateQrCodeImage(QrCodeData);

            AllGroups = await _context.Groups.OrderBy(g => g.name).ToListAsync();

            AssignedGroupIds = await _context.MachineGroups
                .Where(mg => mg.MachineId == Id)
                .Select(mg => mg.GroupId)
                .ToListAsync();

            return Page();
        }

        public async Task<JsonResult> OnPostRegenerateTokenAsync([FromBody] RegenerateTokenRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new JsonResult(new { success = false, message = "Error: User not authenticated." });
            }

            var isAdmin = await _context.Admins.AnyAsync(a => a.UserId == userId);
            if (!isAdmin)
            {
                return new JsonResult(new { success = false, message = "Error: You do not have permission to manage this machine." });
            }

            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.Id == request.id);
            if (machine == null)
            {
                return new JsonResult(new { success = false, message = $"Error: Machine not found." });
            }

            // Regenerate the token
            machine.Token = Guid.NewGuid().ToString("N");
            await _context.SaveChangesAsync();

            // Re-generate the QR code data string
            var QrCodeData = $"{{\"machineId\": {machine.Id}, \"machineToken\": \"{machine.Token}\"}}";

            // Return the new data as JSON
            return new JsonResult(new
            {
                success = true,
                message = "Success: Token regenerated.",
                newMachineId = machine.Id,
                newToken = machine.Token,
                newQrCodeImage = Methods.GenerateQrCodeImage(QrCodeData)
            });
        }
        public class RegenerateTokenRequest
        {
            public long id { get; set; }
        }

        public async Task<JsonResult> OnPostAddGroupAsync([FromBody] AddGroupRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new JsonResult(new { success = false, message = "Error: User not authenticated." });
            }

            IsAdmin = await Methods.IsUserAdminAsync(_context, userId);
            if (!IsAdmin)
            {
                return new JsonResult(new { success = false, message = "Error: You do not have permission to add a group." });
            }

            if (!await _context.Machines.AnyAsync(m => m.Id == request.machineId))
            {
                return new JsonResult(new { success = false, message = "Error: Machine not found." });
            }

            if (!await _context.Groups.AnyAsync(g => g.Id == request.groupId))
            {
                StatusMessage = "Error: Group not found.";
                return new JsonResult(new { success = false, message = "Error: Group not found." });
            }

            var isAlreadyAssigned = await _context.MachineGroups
                .AnyAsync(mg => mg.MachineId == Id && mg.GroupId == request.groupId);

            if (isAlreadyAssigned)
            {
                StatusMessage = "Error: Machine is already assigned to this group.";
                return new JsonResult(new { success = false, message = "Error: Machine is already assigned to this group." });
            }

            var machineGroup = new MachineGroup
            {
                MachineId = request.machineId,
                GroupId = request.groupId
            };

            _context.MachineGroups.Add(machineGroup);
            await _context.SaveChangesAsync();

            return new JsonResult(new 
            { 
                success = true, 
                message = "Success: Group added to machine.",
                groupId = request.groupId.ToString()
            });
        }

        public class AddGroupRequest
        {
            public long machineId { get; set; }
            public long groupId { get; set; }
        }

        public async Task<JsonResult> OnPostRemoveGroupAsync([FromBody] RemoveGroupRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new JsonResult(new { success = false, message = "Error: User not authenticated." });
            }

            IsAdmin = await Methods.IsUserAdminAsync(_context, userId);
            if (!IsAdmin)
            {
                return new JsonResult(new { success = false, message = "Error: You do not have permission to remove a group." });
            }

            var machineGroup = await _context.MachineGroups
                .FirstOrDefaultAsync(mg => mg.MachineId == Id && mg.GroupId == request.groupId);

            if (machineGroup == null)
            {
                StatusMessage = "Error: Assignment not found.";
                return new JsonResult(new { success = false, message = "Error: Assignment not found." });
            }

            _context.MachineGroups.Remove(machineGroup);
            await _context.SaveChangesAsync();

            StatusMessage = "Success: Group removed from machine.";
            return new JsonResult(new
            {
                success = true,
                message = "Success: Group removed from machine.",
                groupId = request.groupId.ToString()
            });
        }

        public class RemoveGroupRequest
        {
            public long machineId { get; set; }
            public long groupId { get; set; }
        }
    }
}