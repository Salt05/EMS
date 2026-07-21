using EMS.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Data;

public class EmsDbContext : DbContext
{
    public EmsDbContext(DbContextOptions<EmsDbContext> options) : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Fluent API can be configured here if Data Annotations are not enough
    }
}
