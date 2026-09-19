using Microsoft.EntityFrameworkCore;
using R10CSharp.Models;

namespace R10CSharp.Data
{
    /// <summary>
    /// EF-Core-Datenbankkontext der Anwendung.
    /// Verwaltet den Zugriff auf den persistenten Fotoindex.
    /// </summary>
    public class R10PhotoContext(DbContextOptions<R10PhotoContext> options) : DbContext(options)
    {
        /// <summary>
        /// Tabelle der indexierten Fotoeinträge.
        /// </summary>
        public DbSet<PhotoIndexEntry> PhotoIndexEntries { get; set; } = null!;
    }
}
