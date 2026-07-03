using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace personal_attendanse_system.Data.Models
{
    public class Attendee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public string UserId { get; set; } // FK to Users
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        public long GroupId { get; set; } // FK to Group
        [ForeignKey("GroupId")]
        public Group Group { get; set; }

        public DateTime attendensDate { get; set; } = DateTime.UtcNow;
        public long? LinkId { get; set; }
        [ForeignKey("LinkId")]
        public Link? Link { get; set; } // Navigation property
    }
}
