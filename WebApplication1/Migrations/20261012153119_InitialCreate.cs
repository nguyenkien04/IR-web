using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Room = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scenes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Buttons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HexCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Bits = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buttons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Buttons_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Time = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SceneId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedules_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SceneButtons",
                columns: table => new
                {
                    SceneId = table.Column<int>(type: "int", nullable: false),
                    ButtonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneButtons", x => new { x.SceneId, x.ButtonId });
                    table.ForeignKey(
                        name: "FK_SceneButtons_Buttons_ButtonId",
                        column: x => x.ButtonId,
                        principalTable: "Buttons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SceneButtons_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Buttons_DeviceId",
                table: "Buttons",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SceneButtons_ButtonId",
                table: "SceneButtons",
                column: "ButtonId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_SceneId",
                table: "Schedules",
                column: "SceneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SceneButtons");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropTable(
                name: "Buttons");

            migrationBuilder.DropTable(
                name: "Scenes");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
