using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace personal_attendanse_system.Data.Models
{
    public class Token
    {
        [Key]
        public string QrCodeData { get; set; } // The unique key embedded in the QR code

        [Required]
        public string UserId { get; set; } // FK to ApplicationUser

        [ForeignKey("UserId")]
        public User User { get; set; }

        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    }
}


