using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fuzz.Domain.Migrations
{
    /// <inheritdoc />
    public partial class RenameAiModelsToFuzzAiModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AiModels",
                table: "AiModels");

            migrationBuilder.RenameTable(
                name: "AiModels",
                newName: "FuzzAiModels");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FuzzAiModels",
                table: "FuzzAiModels",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FuzzAiModels",
                table: "FuzzAiModels");

            migrationBuilder.RenameTable(
                name: "FuzzAiModels",
                newName: "AiModels");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AiModels",
                table: "AiModels",
                column: "Id");
        }
    }
}
