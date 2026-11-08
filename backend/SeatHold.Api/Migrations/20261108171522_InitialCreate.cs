using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeatHold.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "classes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TotalSeats = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_classes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "holds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ClassId = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_holds_classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_holds_ClassId_Status",
                table: "holds",
                columns: new[] { "ClassId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_holds_StudentId",
                table: "holds",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "holds");

            migrationBuilder.DropTable(
                name: "classes");
        }
    }
}
