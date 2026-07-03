using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace personal_attendanse_system.Data.Models
{
    public class Machine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required, MaxLength(256)]
        public string Name { get; set; } = "";
        public DateTime creationDate { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(256)]
        public string Token { get; set; } = Guid.NewGuid().ToString();


        public ICollection<MachineGroup>? MachineGroup { get; set; }
    }
}
