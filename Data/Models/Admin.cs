using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace personal_attendanse_system.Data.Models
{
    public class Admin
    {
        [Key]
        public string UserId { get; set; } // FK to Users
        [ForeignKey("UserId")]
        public User User { get; set; }

        public DateTime assignDate { get; set; } = DateTime.UtcNow;
    }
}
