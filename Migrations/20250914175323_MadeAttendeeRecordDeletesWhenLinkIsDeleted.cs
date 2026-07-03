using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace personalattendansesystem.Migrations
{
    /// <inheritdoc />
    public partial class MadeAttendeeRecordDeletesWhenLinkIsDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendees_Links_LinkId",
                table: "Attendees");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendees_Links_LinkId",
                table: "Attendees",
                column: "LinkId",
                principalTable: "Links",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendees_Links_LinkId",
                table: "Attendees");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendees_Links_LinkId",
                table: "Attendees",
                column: "LinkId",
                principalTable: "Links",
                principalColumn: "Id");
        }
    }
}
