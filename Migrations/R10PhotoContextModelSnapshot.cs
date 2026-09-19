using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using R10CSharp.Data;

#nullable disable

namespace R10CSharp.Migrations
{
    [DbContext(typeof(R10PhotoContext))]
    /// <summary>
    /// EF-Core-Modellsnapshot des aktuellen Datenbankmodells.
    /// Dient EF zur Ermittlung künftiger Schemaänderungen.
    /// </summary>
    partial class R10PhotoContextModelSnapshot : ModelSnapshot
    {
        /// <summary>
        /// Beschreibt das aktuell bekannte EF-Core-Modell.
        /// </summary>
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("R10CSharp.Models.PhotoIndexEntry", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int");
                b.Property<string>("Category").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<string>("FileName").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<string>("FilePath").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<string>("RawOrJpg").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<string>("Series").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<DateTime>("Timestamp").HasColumnType("datetime2");
                b.Property<string>("Tags").IsRequired().HasColumnType("nvarchar(max)");
                b.Property<string>("Thumbnail").IsRequired().HasColumnType("nvarchar(max)");

                b.HasKey("Id");

                b.ToTable("PhotoIndexEntries");
            });
        }
    }
}
