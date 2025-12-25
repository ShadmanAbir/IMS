using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using IMS.Api.Application.Common.Interfaces;
using IMS.Api.Domain.Common;
using IMS.Api.Domain.Aggregates;
using IMS.Api.Domain.Entities;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<
    ApplicationUser,
    ApplicationRole,
    Guid,
    ApplicationUserClaim,
    ApplicationUserRole,
    ApplicationUserLogin,
    ApplicationRoleClaim,
    ApplicationUserToken>, IUnitOfWork
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Aggregate Roots
    public DbSet<Product> Products { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<Reservation> Reservations { get; set; }

    // Entities
    public DbSet<Variant> Variants { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<ProductAttribute> ProductAttributes { get; set; }
    public DbSet<VariantAttribute> VariantAttributes { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    
    // Cache entities for materialized views
    public DbSet<DashboardMetricsCache> DashboardMetricsCache { get; set; }
    public DbSet<StockMovementRatesCache> StockMovementRatesCache { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // This configures Identity tables
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        // Configure global query filters for multi-tenancy
        ConfigureGlobalFilters(modelBuilder);
        
        // Configure Identity entities
        ConfigureIdentityEntities(modelBuilder);
    }

    private void ConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        // Add tenant filter for Product
        modelBuilder.Entity<Product>().HasQueryFilter(p => 
            (_tenantContext.CurrentTenantId == null || p.TenantId == _tenantContext.CurrentTenantId) && !p.IsDeleted);
        
        // Add soft delete filter for InventoryItem
        modelBuilder.Entity<InventoryItem>().HasQueryFilter(i => !i.IsDeleted);
        
        // Add soft delete filter for Variant
        modelBuilder.Entity<Variant>().HasQueryFilter(v => !v.IsDeleted);
        
        // Add soft delete filter for ProductAttribute
        modelBuilder.Entity<ProductAttribute>().HasQueryFilter(pa => !pa.IsDeleted);
        
        // Add soft delete filter for VariantAttribute
        modelBuilder.Entity<VariantAttribute>().HasQueryFilter(va => !va.IsDeleted);
        
        // Add tenant filter for Alert
        modelBuilder.Entity<Alert>().HasQueryFilter(a => 
            (_tenantContext.CurrentTenantId == null || a.TenantId == _tenantContext.CurrentTenantId) && !a.IsDeleted);

        // Add tenant filter for ApplicationUser
        modelBuilder.Entity<ApplicationUser>().HasQueryFilter(u => 
            (_tenantContext.CurrentTenantId == null || u.TenantId == _tenantContext.CurrentTenantId) && u.IsActive);
    }

    private void ConfigureIdentityEntities(ModelBuilder modelBuilder)
    {
        // Configure ApplicationUser
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.TenantId)
                .HasConversion(
                    v => v.Value,
                    v => TenantId.Create(v))
                .IsRequired();

            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.Email })
                .IsUnique()
                .HasDatabaseName("IX_ApplicationUsers_TenantId_Email");
        });

        // Configure ApplicationRole
        modelBuilder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(e => e.Description)
                .HasMaxLength(500);
        });

        // Configure ApplicationUserRole
        modelBuilder.Entity<ApplicationUserRole>(entity =>
        {
            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);
        });

        // Configure ApplicationRoleClaim
        modelBuilder.Entity<ApplicationRoleClaim>(entity =>
        {
            entity.HasOne(rc => rc.Role)
                .WithMany(r => r.RoleClaims)
                .HasForeignKey(rc => rc.RoleId);
        });

        // Configure ApplicationUserClaim
        modelBuilder.Entity<ApplicationUserClaim>(entity =>
        {
            entity.HasOne(uc => uc.User)
                .WithMany()
                .HasForeignKey(uc => uc.UserId);
        });

        // Configure table names to avoid conflicts
        modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");
        modelBuilder.Entity<ApplicationRole>().ToTable("AspNetRoles");
        modelBuilder.Entity<ApplicationUserRole>().ToTable("AspNetUserRoles");
        modelBuilder.Entity<ApplicationRoleClaim>().ToTable("AspNetRoleClaims");
        modelBuilder.Entity<ApplicationUserClaim>().ToTable("AspNetUserClaims");
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.RollbackTransactionAsync(cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Add audit fields and tenant information
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Entity<Guid> && (
                e.State == EntityState.Added ||
                e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            // Set tenant ID for new entities
            if (entityEntry.State == EntityState.Added && _tenantContext.CurrentTenantId != null)
            {
                var tenantProperty = entityEntry.Entity.GetType().GetProperty("TenantId");
                if (tenantProperty != null && tenantProperty.CanWrite)
                {
                    tenantProperty.SetValue(entityEntry.Entity, _tenantContext.CurrentTenantId);
                }
            }

            if (entityEntry.State == EntityState.Added)
            {
                if (entityEntry.Entity.GetType().GetProperty("CreatedAtUtc") != null)
                {
                    entityEntry.Property("CreatedAtUtc").CurrentValue = DateTime.UtcNow;
                }
            }

            if (entityEntry.State == EntityState.Modified)
            {
                if (entityEntry.Entity.GetType().GetProperty("UpdatedAtUtc") != null)
                {
                    entityEntry.Property("UpdatedAtUtc").CurrentValue = DateTime.UtcNow;
                }
            }
        }

        // Handle soft deletes - convert hard deletes to soft deletes
        var deletedEntries = ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Deleted && e.Entity is ISoftDeletable)
            .ToList();

        foreach (var entry in deletedEntries)
        {
            entry.State = EntityState.Modified;
            var softDeletable = (ISoftDeletable)entry.Entity;
            
            // Use a system user ID if no current user is available
            var systemUserId = UserId.Create(Guid.Empty); // This should be replaced with actual system user logic
            softDeletable.SoftDelete(systemUserId);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}