using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace personalattendansesystem.Migrations
{
    /// <inheritdoc />
    public partial class AddedGroupStaffTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupStaffs",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    GroupId = table.Column<long>(type: "INTEGER", nullable: false),
                    joinDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    canCreateScheduledLink = table.Column<bool>(type: "INTEGER", nullable: false),
                    canUpdateScheduledLink = table.Column<bool>(type: "INTEGER", nullable: false),
                    canDeleteScheduledLink = table.Column<bool>(type: "INTEGER", nullable: false),
                    canCreateLink = table.Column<bool>(type: "INTEGER", nullable: false),
                    canUpdateLink = table.Column<bool>(type: "INTEGER", nullable: false),
                    canDeleteLink = table.Column<bool>(type: "INTEGER", nullable: false),
                    canViewScheduledLinks = table.Column<bool>(type: "INTEGER", nullable: false),
                    canViewLinks = table.Column<bool>(type: "INTEGER", nullable: false),
                    canViewLinksQRCodes = table.Column<bool>(type: "INTEGER", nullable: false),
                    canViewLinksAttendees = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupStaffs", x => new { x.UserId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_GroupStaffs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupStaffs_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupStaffs_GroupId",
                table: "GroupStaffs",
                column: "GroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupStaffs");
        }
    }
}
