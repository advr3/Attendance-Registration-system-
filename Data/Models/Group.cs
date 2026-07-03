using Humanizer;
using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace personal_attendanse_system.Data.Models
{
    public class Group
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required, MaxLength(256)]
        public string name { get; set; } = "";

        public string description { get; set; } = "";

        public bool isPublic { get; set; } = false;

        public DateTime creationDate { get; set; } = DateTime.UtcNow;

        public bool limitLinkActiveTime { get; set; } = false;
        // the time that a link can be active in
        public string DaysOfWeek { get; set; } = ""; // Stores selected days (e.g., "Sunday,Friday")
        public DateTime StartTime { get; set; } = DateTime.UtcNow; // The time the link should be created
        public DateTime EndTime { get; set; } = DateTime.UtcNow;// The time the link should expire

        public ICollection<Attendee>? Attendees { get; set; } // Direct attendees (if not via GroupMember/Link)

        public ICollection<Link>? Links { get; set; }

        public ICollection<GroupMember>? GroupMembers { get; set; }
        public ICollection<GroupStaff>? GroupStaffs { get; set; }
        public ICollection<RecurringTask>? RecurringTasks { get; set; }
        public ICollection<MachineGroup>? MachineGroup { get; set; }
    }
}
