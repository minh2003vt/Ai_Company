using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntitiesWithNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rule",
                table: "AI_Configurations");

            migrationBuilder.RenameColumn(
                name: "KnowledgeSource",
                table: "AI_Configurations",
                newName: "RulesJson");

            migrationBuilder.AddColumn<Guid>(
                name: "AIConfigureId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxOutputTokens",
                table: "AI_Configurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "AI_Configurations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RagTopK",
                table: "AI_Configurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "Temperature",
                table: "AI_Configurations",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<bool>(
                name: "UseRag",
                table: "AI_Configurations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseStreaming",
                table: "AI_Configurations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AIConfigureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExternalSessionId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatSessions_AI_Configurations_AIConfigureId",
                        column: x => x.AIConfigureId,
                        principalTable: "AI_Configurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MetaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AIConfigureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeSources_AI_Configurations_AIConfigureId",
                        column: x => x.AIConfigureId,
                        principalTable: "AI_Configurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KnowledgeSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MetaJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_KnowledgeSources_KnowledgeSourceId",
                        column: x => x.KnowledgeSourceId,
                        principalTable: "KnowledgeSources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_AIConfigureId",
                table: "Users",
                column: "AIConfigureId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatSessionId",
                table: "ChatMessages",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_KnowledgeSourceId",
                table: "ChatMessages",
                column: "KnowledgeSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_AIConfigureId",
                table: "ChatSessions",
                column: "AIConfigureId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_UserId",
                table: "ChatSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeSources_AIConfigureId",
                table: "KnowledgeSources",
                column: "AIConfigureId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_AI_Configurations_AIConfigureId",
                table: "Users",
                column: "AIConfigureId",
                principalTable: "AI_Configurations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_AI_Configurations_AIConfigureId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.DropTable(
                name: "KnowledgeSources");

            migrationBuilder.DropIndex(
                name: "IX_Users_AIConfigureId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DepartmentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AIConfigureId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MaxOutputTokens",
                table: "AI_Configurations");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "AI_Configurations");

            migrationBuilder.DropColumn(
                name: "RagTopK",
                table: "AI_Configurations");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "AI_Configurations");

            migrationBuilder.DropColumn(
                name: "UseRag",
                table: "AI_Configurations");

            migrationBuilder.DropColumn(
                name: "UseStreaming",
                table: "AI_Configurations");

            migrationBuilder.RenameColumn(
                name: "RulesJson",
                table: "AI_Configurations",
                newName: "KnowledgeSource");

            migrationBuilder.AddColumn<string>(
                name: "Rule",
                table: "AI_Configurations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }
    }
}
