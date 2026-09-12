using Microsoft.EntityFrameworkCore;
using R10CSharp.Models;

namespace R10CSharp.Data
{
    public class R10PhotoContext : DbContext
    {
        public R10PhotoContext(DbContextOptions<R10PhotoContext> options) : base(options)
        {
        }

        public DbSet<PhotoIndexEntry> PhotoIndexEntries { get; set; } = null!;
    }
}
