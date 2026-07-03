using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace personalattendansesystem.Migrations
{
    /// <inheritdoc />
    public partial class AddedPremssionToRecurringTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "canAttendThroughMachineScan",
                table: "RecurringTasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "canAttendThroughStaffScan",
                table: "RecurringTasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "canAttendThroughUserScan",
                table: "RecurringTasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "canAttendThroughMachineScan",
                table: "RecurringTasks");

            migrationBuilder.DropColumn(
                name: "canAttendThroughStaffScan",
                table: "RecurringTasks");

            migrationBuilder.DropColumn(
                name: "canAttendThroughUserScan",
                table: "RecurringTasks");
        }
    }
}
