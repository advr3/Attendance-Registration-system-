using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using personal_attendanse_system.Data.Models;
using System.Reflection.Emit;

namespace personal_attendanse_system.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Attendee> Attendees { get; set; }
        public DbSet<Link> Links { get; set; }
        public DbSet<Token> Tokens { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<GroupStaff> GroupStaffs { get; set; }
        public DbSet<RecurringTask> RecurringTasks { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<MachineGroup> MachineGroups { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // === Admin ===
            builder.Entity<Admin>()
                .HasOne(a => a.User)
                .WithMany(u => u.Admins) // Add ICollection<Admin> GlobalAdminProfile to Users
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade); // If User is deleted, delete Admin record


            // === Attendee ===
            builder.Entity<Attendee>()
                .HasOne(a => a.User)
                .WithMany(u => u.Attendees)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Attendee>()
                .HasOne(a => a.Group)
                .WithMany(g => g.Attendees) // Add ICollection<Attendee> Attendees to Group
                .HasForeignKey(a => a.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Attendee>()
                .HasOne(a => a.Link) // Attendee has one Link
                .WithMany(l => l.Attendees) // Link has many Attendees
                .HasForeignKey(a => a.LinkId) // The foreign key property is LinkId
                .IsRequired(false) // LinkId is nullable
                .OnDelete(DeleteBehavior.Cascade);


            // === Link ===
            builder.Entity<Link>()
                .HasOne(tl => tl.Group)
                .WithMany(g => g.Links) // Add ICollection<TmpLink> TmpLinks to Group
                .HasForeignKey(tl => tl.GroupId)
                .OnDelete(DeleteBehavior.Cascade);


            // === Token ===
            builder.Entity<Token>()
            .HasOne(t => t.User)
            .WithMany(u => u.Tokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);


            // === GroupStaffs ===
            builder.Entity<GroupStaff>()
                .HasKey(gm => new { gm.UserId, gm.GroupId });

            builder.Entity<GroupStaff>()
                .HasOne(gm => gm.User)
                .WithMany(u => u.GroupStaffs) // Now you can use the navigation property
                .HasForeignKey(gm => gm.UserId);

            builder.Entity<GroupStaff>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.GroupStaffs)
                .HasForeignKey(gm => gm.GroupId);


            // === GroupMember ===
            builder.Entity<GroupMember>()
                .HasKey(gm => new { gm.UserId, gm.GroupId });

            builder.Entity<GroupMember>()
                .HasOne(gm => gm.User)
                .WithMany(u => u.GroupMembers) // Now you can use the navigation property
                .HasForeignKey(gm => gm.UserId);

            builder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.GroupMembers)
                .HasForeignKey(gm => gm.GroupId);


            // === RecurringTask ===
            builder.Entity<RecurringTask>()
                .HasOne(t => t.User)
                .WithMany() // You might need to specify a navigation property here if it exists on ApplicationUser
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // This is the crucial line for cascade delete

            builder.Entity<RecurringTask>()
                .HasOne(t => t.Group)
                .WithMany(r => r.RecurringTasks) // Assuming no navigation property on the Group side
                .HasForeignKey(t => t.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // This is the crucial line for cascade delete




            // === MachineGroup ===
            builder.Entity<MachineGroup>()
                .HasKey(gm => new { gm.MachineId, gm.GroupId });

            builder.Entity<MachineGroup>()
                .HasOne(gm => gm.Machine)
                .WithMany(u => u.MachineGroup) // Now you can use the navigation property
                .HasForeignKey(gm => gm.MachineId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MachineGroup>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.MachineGroup)
                .HasForeignKey(gm => gm.GroupId);
        }
    }
}