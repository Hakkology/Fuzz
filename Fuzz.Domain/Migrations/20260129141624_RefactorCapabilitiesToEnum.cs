using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fuzz.Domain.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCapabilitiesToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add new columns
            migrationBuilder.AddColumn<int>(
                name: "Capabilities",
                table: "FuzzAiModels",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "FuzzAIConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // 2. Migrate existing data (PostgreSQL)
            // FuzzAiModels: IsTextCapable(1) | IsVisualRecognition(2)
            // FuzzAIConfigs: IsVisualRecognition -> If true Visual(2), else Text(1)
            migrationBuilder.Sql(@"
                UPDATE ""FuzzAiModels""
                SET ""Capabilities"" = 
                    (CASE WHEN ""IsTextCapable"" THEN 1 ELSE 0 END) + 
                    (CASE WHEN ""IsVisualRecognition"" THEN 2 ELSE 0 END);

                UPDATE ""FuzzAIConfigs""
                SET ""Mode"" = 
                    (CASE WHEN ""IsVisualRecognition"" THEN 2 ELSE 1 END);
            ");

            // 3. Drop old columns
            migrationBuilder.DropColumn(
                name: "IsTextCapable",
                table: "FuzzAiModels");

            migrationBuilder.DropColumn(
                name: "IsVisualRecognition",
                table: "FuzzAiModels");

            migrationBuilder.DropColumn(
                name: "IsVisualRecognition",
                table: "FuzzAIConfigs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Capabilities",
                table: "FuzzAiModels");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "FuzzAIConfigs");

            migrationBuilder.AddColumn<bool>(
                name: "IsTextCapable",
                table: "FuzzAiModels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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
    }
}
