using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using personal_attendanse_system.Data.Models;

namespace personal_attendanse_system.Data.Models
{
    public class User : IdentityUser
    {
        // Add custom properties here if needed

        // Navigation properties for relationships
        public ICollection<Admin>? Admins { get; set; }
        public ICollection<Token>? Tokens { get; set; }
        public ICollection<GroupMember>? GroupMembers { get; set; }
        public ICollection<GroupStaff>? GroupStaffs { get; set; }
        public ICollection<Attendee>? Attendees { get; set; }
    }
}