using Microsoft.EntityFrameworkCore.Migrations;

namespace Cloud.Migrations
{
    public partial class ShowKnownFileExtensionsProp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowKnownFileExtensions",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowKnownFileExtensions",
                table: "Users");
        }
    }
}
