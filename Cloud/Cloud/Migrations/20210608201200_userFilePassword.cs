using Microsoft.EntityFrameworkCore.Migrations;

namespace Cloud.Migrations
{
    public partial class userFilePassword : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilePassword",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePassword",
                table: "Users");
        }
    }
}
