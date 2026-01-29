using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fuzz.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddVisualRecognitionFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVisualRecognition",
                table: "FuzzAiModels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisualRecognition",
                table: "FuzzAIConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVisualRecognition",
                table: "FuzzAiModels");

            migrationBuilder.DropColumn(
                name: "IsVisualRecognition",
                table: "FuzzAIConfigs");
        }
    }
}
