using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace personal_attendanse_system.Data.Models
{
    public class GroupMember
    {
        
        [Required]
        public string UserId { get; set; } // FK to Users
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        public long GroupId { get; set; } // FK to Group
        [ForeignKey("GroupId")]
        public Group Group { get; set; }

        public DateTime joinDate { get; set; } = DateTime.UtcNow;
    }
}
