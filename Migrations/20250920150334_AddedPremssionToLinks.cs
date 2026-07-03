using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace personalattendansesystem.Migrations
{
    /// <inheritdoc />
    public partial class AddedPremssionToLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "canAttendThroughMachineScan",
                table: "Links",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "canAttendThroughStaffScan",
                table: "Links",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "canAttendThroughUserScan",
                table: "Links",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "canAttendThroughMachineScan",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "canAttendThroughStaffScan",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "canAttendThroughUserScan",
                table: "Links");
        }
    }
}
