using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using personal_attendanse_system.Data.Models;
using personal_attendanse_system.Data;
using personal_attendanse_system.Globals;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;

namespace personal_attendanse_system.Pages.Groups.Machine
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Data.Models.Machine> Machines { get; set; } = new List<Data.Models.Machine>();

        public bool IsAdmin { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                IsAdmin = await _context.Admins.AnyAsync(a => a.UserId == userId);
            }

            if (IsAdmin)
            {
                Machines = await _context.Machines.OrderBy(m => m.Name).ToListAsync();
            }
        }

        public async Task<JsonResult> OnPostCreateMachineAsync([FromBody] MachineCreateRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return new JsonResult(new { success = false, message = "Error: User not authenticated." });
            }

            IsAdmin = await Methods.IsUserAdminAsync(_context, userId);
            if (!IsAdmin)
            {
                //Response.StatusCode = 403; // Forbidden
                return new JsonResult(new { success = false, message = "Error: You do not have permission to create a machine." });
            }

            // Create a new machine
            var newMachine = new Data.Models.Machine
            {
                Name = request.name,
                creationDate = DateTime.UtcNow
            };

            _context.Machines.Add(newMachine);
            await _context.SaveChangesAsync();

            return new JsonResult(new 
            { 
                success = true, 
                message = $"Machine '{newMachine.Name}' created successfully.", 
                machineId = newMachine.Id,
                machineName = newMachine.Name
            });
            
        }

        public class MachineCreateRequest
        {
            public string name { get; set; }
        }

        public async Task<JsonResult> OnPostDeleteMachineAsync([FromBody] MachineDeleteRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            if (userId == null)
            {
                return new JsonResult(new { success = false, message = "Error: User not authenticated." });
            }

            IsAdmin = await Methods.IsUserAdminAsync(_context, userId);
            if (!IsAdmin)
            {
                return new JsonResult(new { success = false, message = "Error: You do not have permission to create a machine." });
            }

            var machineToDelete = await _context.Machines
                .FirstOrDefaultAsync(m => m.Id == request.id);

            if (machineToDelete == null)
            {
                return new JsonResult(new { success = false, message = "Error: Machine not found for deletion." });
            }

            _context.Machines.Remove(machineToDelete);
            await _context.SaveChangesAsync(); // All associated MachineGroup records will be deleted here

            return new JsonResult(new
            {
                success = true,
                message = $"Machine '{machineToDelete.Name}' deleted successfully!",
                deletedMachineId = machineToDelete.Id
            });
        }
        public class MachineDeleteRequest
        {
            public long id { get; set; }
        }


    }
}
