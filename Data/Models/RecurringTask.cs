using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace personal_attendanse_system.Data.Models
{
    public class RecurringTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; } // Primary key

        // Foreign key to link this task to the user who created it
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } // Navigation property to the User

        // Foreign key to link this task to a specific group
        public long GroupId { get; set; }
        [ForeignKey("GroupId")]
        public Group Group { get; set; } // Navigation property to the Group

        // Scheduling properties
        [MaxLength(256)]
        public string Name { get; set; } // A user-friendly name for the task
        public string DaysOfWeek { get; set; } // Stores selected days (e.g., "Sunday,Friday")
        public DateTime StartTime { get; set; } // The time the link should be created
        public DateTime EndTime { get; set; } // The time the link should expire
        public bool canAttendThroughUserScan { get; set; } = false;
        public bool canAttendThroughStaffScan { get; set; } = false;
        public bool canAttendThroughMachineScan { get; set; } = false;

        // Optional: a reference to the last created link if needed for tracking
        /*
        public int? LastCreatedLinkId { get; set; }
        [ForeignKey("LastCreatedLinkId")]
        public Link LastCreatedLink { get; set; }
        */
    }
}
