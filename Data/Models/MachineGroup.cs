using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace personal_attendanse_system.Data.Models
{
    public class MachineGroup
    {
        [Required]
        public long MachineId { get; set; } // FK to Users
        [ForeignKey("MachineId")]
        public Machine Machine { get; set; }

        [Required]
        public long GroupId { get; set; } // FK to Group
        [ForeignKey("GroupId")]
        public Group Group { get; set; }


    }
}
