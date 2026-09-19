using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R10CSharp.Migrations
{
    /// <summary>
    /// Erste EF-Core-Migration zum Anlegen der Tabelle für Fotoindexeinträge.
    /// </summary>
    public partial class InitialCreate : Migration
    {
        /// <summary>
        /// Legt die initiale Datenbankstruktur an.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhotoIndexEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawOrJpg = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Series = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Thumbnail = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoIndexEntries", x => x.Id);
                });
        }

        /// <summary>
        /// Entfernt die initiale Datenbankstruktur wieder.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhotoIndexEntries");
        }
    }
}
