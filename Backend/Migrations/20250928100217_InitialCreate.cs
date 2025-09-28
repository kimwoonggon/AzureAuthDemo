using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AzureId = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeviceInfo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Documents",
                columns: new[] { "Id", "Category", "Content", "CreatedAt", "Title", "UserId" },
                values: new object[,]
                {
                    { 1, "Authentication", "Azure AD 인증 설정 방법: 1. Azure Portal 접속 2. App Registration 생성 3. Client ID 및 Tenant ID 확인 4. Redirect URI 설정", new DateTime(2025, 9, 28, 10, 2, 15, 675, DateTimeKind.Utc).AddTicks(7000), "Azure AD 가이드", 1 },
                    { 2, "Security", "JWT는 Header.Payload.Signature 구조로 되어 있으며, Base64로 인코딩됩니다. Access Token은 짧은 수명, Refresh Token은 긴 수명을 가집니다.", new DateTime(2025, 9, 28, 10, 2, 15, 675, DateTimeKind.Utc).AddTicks(7010), "JWT 토큰 이해하기", 1 },
                    { 3, "Authentication", "PKCE(Proof Key for Code Exchange)는 Code Verifier와 Code Challenge를 사용하여 Authorization Code 가로채기 공격을 방지합니다.", new DateTime(2025, 9, 28, 10, 2, 15, 675, DateTimeKind.Utc).AddTicks(7010), "PKCE 플로우", 1 },
                    { 4, "Security", "OAuth 2.0은 인증 프로토콜로 Resource Owner, Client, Authorization Server, Resource Server의 4가지 역할로 구성됩니다.", new DateTime(2025, 9, 28, 10, 2, 15, 675, DateTimeKind.Utc).AddTicks(7010), "OAuth 2.0 기본 개념", 1 },
                    { 5, "Development", "Microsoft Authentication Library는 Azure AD 인증을 쉽게 구현할 수 있게 해주는 JavaScript 라이브러리입니다.", new DateTime(2025, 9, 28, 10, 2, 15, 675, DateTimeKind.Utc).AddTicks(7010), "MSAL.js 사용법", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
