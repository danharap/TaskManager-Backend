using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendW2Proj.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannedCompletionDateToTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "plannedCompletionDate",
                table: "Tasks",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "plannedCompletionDate",
                table: "Tasks");
        }
    }
}
