using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace personal_attendanse_system.Data.Models
{
    public class Link
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public long GroupId { get; set; } // FK to Group
        [ForeignKey("GroupId")]
        public Group Group { get; set; }

        [Required, MaxLength(256)]
        public string LinkURL { get; set; } = Guid.NewGuid().ToString();
        public DateTime creationDate { get; set; } = DateTime.UtcNow;
        public DateTime activetionDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public DateTime expirationDate { get; set; } // When the link becomes invalid

        public bool canAttendThroughUserScan { get; set; } = false;
        public bool canAttendThroughStaffScan { get; set; } = false;
        public bool canAttendThroughMachineScan { get; set; } = false;

        public ICollection<Attendee>? Attendees { get; set; }
    }
}
