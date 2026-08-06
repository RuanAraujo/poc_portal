using Documentation.Application.Abstractions.Persistence;
using Documentation.Domain.Entities;
using Documentation.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Documentation.Infrastructure.Persistence;

public sealed class DocumentationDbContext(DbContextOptions<DocumentationDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<ApiDocumentation> ApiDocumentations => Set<ApiDocumentation>();

    public DbSet<DocumentationVersion> DocumentationVersions => Set<DocumentationVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("documentation");

        modelBuilder.Entity<ApiDocumentation>(entity =>
        {
            entity.ToTable("api_documentations");
            entity.HasKey(documentation => documentation.Id);
            entity.Property(documentation => documentation.ApiId).HasMaxLength(200).IsRequired();
            entity.Property(documentation => documentation.Name).HasMaxLength(300).IsRequired();
            entity.Property(documentation => documentation.CreatedAtUtc).IsRequired();
            entity.HasIndex(documentation => documentation.ApiId).IsUnique();

            entity.HasMany(documentation => documentation.Versions)
                .WithOne(version => version.Documentation)
                .HasForeignKey(version => version.DocumentationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentationVersion>(entity =>
        {
            entity.ToTable("documentation_versions");
            entity.HasKey(version => version.Id);
            entity.Property(version => version.Version).HasMaxLength(100).IsRequired();
            entity.Property(version => version.Environment).HasMaxLength(100).IsRequired();
            entity.Property(version => version.Format).HasMaxLength(50).IsRequired();
            entity.Property(version => version.Content).HasColumnType("text").IsRequired();
            entity.Property(version => version.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(version => version.LastError).HasColumnType("text");
            entity.Property(version => version.CreatedAtUtc).IsRequired();
            entity.HasIndex(version => new { version.DocumentationId, version.Version, version.Environment }).IsUnique();
        });
    }
}
