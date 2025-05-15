using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendW2Proj.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToSubTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "SubTasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                table: "SubTasks");
        }
    }
}
