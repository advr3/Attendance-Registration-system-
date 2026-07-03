using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace personalattendansesystem.Migrations
{
    /// <inheritdoc />
    public partial class RemovedViewRelatedAttrebutesGroupStaffTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "canViewLinks",
                table: "GroupStaffs");

            migrationBuilder.DropColumn(
                name: "canViewScheduledLinks",
                table: "GroupStaffs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "canViewLinks",
                table: "GroupStaffs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "canViewScheduledLinks",
                table: "GroupStaffs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
