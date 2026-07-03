using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using personal_attendanse_system.Data.Models;
using personal_attendanse_system.Data;
using QRCoder;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using personal_attendanse_system.Globals;
using Microsoft.AspNetCore.Components.Routing;

namespace personal_attendanse_system.Pages
{
    public class MyQRCodeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MyQRCodeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserQrCodeData { get; set; }
        public string QrCodeImage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Find the user's token
            var token = await _context.Tokens.FirstOrDefaultAsync(t => t.UserId == userId);

            if (token != null)
            {
                UserQrCodeData = Url.PageLink("/MyQRCode", null, new { token = token.QrCodeData });
                QrCodeImage = Methods.GenerateQrCodeImage(UserQrCodeData);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Check if a token already exists for the user
            var existingToken = await _context.Tokens.FirstOrDefaultAsync(t => t.UserId == userId);

            if (existingToken != null)
            {
                _context.Tokens.Remove(existingToken);
            }

            // Generate new QR code data
            var newQrCodeData = Guid.NewGuid().ToString("N");
            var newToken = new Token
            {
                QrCodeData = newQrCodeData,
                UserId = userId
            };

            _context.Tokens.Add(newToken);
            await _context.SaveChangesAsync();

            // Set the public properties to display the new QR code
            
            UserQrCodeData = Url.PageLink("/MyQRCode", null, new { token = newQrCodeData });
            QrCodeImage = Methods.GenerateQrCodeImage(UserQrCodeData);

            return Page();
        }

    }
}
