using Microsoft.EntityFrameworkCore.Migrations;

namespace Cloud.Migrations
{
    public partial class addTotptoggle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TotpActive",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotpActive",
                table: "Users");
        }
    }
}
