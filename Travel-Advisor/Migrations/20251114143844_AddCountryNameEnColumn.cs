using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travel_Advisor.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryNameEnColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryNameEn",
                table: "Destinations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryNameEn",
                table: "Destinations");
        }
    }
}
