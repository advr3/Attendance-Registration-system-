using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace personalattendansesystem.Migrations
{
    /// <inheritdoc />
    public partial class ChangedGroupStaffTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "canViewLinksAttendees",
                table: "GroupStaffs");

            migrationBuilder.DropColumn(
                name: "canViewLinksQRCodes",
                table: "GroupStaffs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "canViewLinksAttendees",
                table: "GroupStaffs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "canViewLinksQRCodes",
                table: "GroupStaffs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
