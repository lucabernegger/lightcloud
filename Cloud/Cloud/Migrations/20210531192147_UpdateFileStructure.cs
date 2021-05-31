using Microsoft.EntityFrameworkCore.Migrations;

namespace Cloud.Migrations
{
    public partial class UpdateFileStructure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Files",
                newName: "Path");

            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "Files",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileIdentifier",
                table: "Files",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "FileIdentifier",
                table: "Files");

            migrationBuilder.RenameColumn(
                name: "Path",
                table: "Files",
                newName: "Name");
        }
    }
}
