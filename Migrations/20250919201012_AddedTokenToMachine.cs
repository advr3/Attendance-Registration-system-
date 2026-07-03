using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace personalattendansesystem.Migrations
{
    /// <inheritdoc />
    public partial class AddedTokenToMachine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "Machines",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Token",
                table: "Machines");
        }
    }
}
