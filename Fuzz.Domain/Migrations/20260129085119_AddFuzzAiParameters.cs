using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fuzz.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddFuzzAiParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FuzzAIParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FuzzAiConfigId = table.Column<int>(type: "integer", nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: false),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    TopP = table.Column<double>(type: "double precision", nullable: false),
                    FrequencyPenalty = table.Column<double>(type: "double precision", nullable: false),
                    PresencePenalty = table.Column<double>(type: "double precision", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuzzAIParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuzzAIParameters_FuzzAIConfigs_FuzzAiConfigId",
                        column: x => x.FuzzAiConfigId,
                        principalTable: "FuzzAIConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FuzzAIParameters_FuzzAiConfigId",
                table: "FuzzAIParameters",
                column: "FuzzAiConfigId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuzzAIParameters");
        }
    }
}
