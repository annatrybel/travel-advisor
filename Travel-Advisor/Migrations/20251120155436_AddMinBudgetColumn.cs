using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel_Advisor.Migrations
{
    /// <inheritdoc />
    public partial class AddMinBudgetColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinBudget",
                table: "Destinations",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinBudget",
                table: "Destinations");
        }
    }
}
