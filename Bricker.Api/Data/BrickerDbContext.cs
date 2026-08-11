using Bricker.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bricker.Api.Data;

public sealed class BrickerDbContext(DbContextOptions<BrickerDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Listing> Listings => Set<Listing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(80).IsRequired();
            entity.Property(category => category.Slug).HasMaxLength(80).IsRequired();
            entity.HasIndex(category => category.Slug).IsUnique();
            entity.HasData(
                new Category { Id = SeedIds.Revestimentos, Name = "Revestimentos", Slug = "revestimentos", IsActive = true },
                new Category { Id = SeedIds.Madeira, Name = "Madeira", Slug = "madeira", IsActive = true },
                new Category { Id = SeedIds.Hidraulica, Name = "Hidráulica", Slug = "hidraulica", IsActive = true },
                new Category { Id = SeedIds.Eletrica, Name = "Elétrica", Slug = "eletrica", IsActive = true },
                new Category { Id = SeedIds.Ferragens, Name = "Ferragens", Slug = "ferragens", IsActive = true });
        });

        modelBuilder.Entity<Listing>(entity =>
        {
            entity.ToTable("Listings");
            entity.HasKey(listing => listing.Id);
            entity.Property(listing => listing.Title).HasMaxLength(160).IsRequired();
            entity.Property(listing => listing.Description).HasMaxLength(2_000).IsRequired();
            entity.Property(listing => listing.Price).HasPrecision(12, 2);
            entity.Property(listing => listing.Quantity).HasPrecision(12, 2);
            entity.Property(listing => listing.Unit).HasMaxLength(24).IsRequired();
            entity.Property(listing => listing.City).HasMaxLength(100).IsRequired();
            entity.Property(listing => listing.State).HasMaxLength(2).IsRequired();
            entity.Property(listing => listing.SellerDisplayName).HasMaxLength(100).IsRequired();
            entity.Property(listing => listing.SellerId).HasMaxLength(450);
            entity.Property(listing => listing.RowVersion).IsRowVersion();
            entity.HasIndex(listing => new { listing.Status, listing.CategoryId, listing.City, listing.State });
            entity.HasOne(listing => listing.Category)
                .WithMany(category => category.Listings)
                .HasForeignKey(listing => listing.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(listing => listing.Seller)
                .WithMany()
                .HasForeignKey(listing => listing.SellerId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasData(
                new Listing
                {
                    Id = SeedIds.Porcelanato,
                    CategoryId = SeedIds.Revestimentos,
                    Title = "Porcelanato cinza 60 x 60",
                    Description = "Lote excedente de porcelanato acetinado, armazenado em local coberto.",
                    Price = 42m,
                    Unit = "m²",
                    Quantity = 18m,
                    Condition = MaterialCondition.Excellent,
                    Status = ListingStatus.Active,
                    City = "Itajaí",
                    State = "SC",
                    SellerDisplayName = "Construtora local",
                    CreatedAtUtc = SeedIds.CreatedAtUtc
                },
                new Listing
                {
                    Id = SeedIds.Portas,
                    CategoryId = SeedIds.Madeira,
                    Title = "Portas de madeira maciça",
                    Description = "Portas novas, sem uso, excedentes de reforma residencial.",
                    Price = 380m,
                    Unit = "unidade",
                    Quantity = 3m,
                    Condition = MaterialCondition.Excellent,
                    Status = ListingStatus.Active,
                    City = "Balneário Camboriú",
                    State = "SC",
                    SellerDisplayName = "Marcenaria parceira",
                    CreatedAtUtc = SeedIds.CreatedAtUtc
                },
                new Listing
                {
                    Id = SeedIds.Tijolos,
                    CategoryId = SeedIds.Revestimentos,
                    Title = "Tijolo ecológico",
                    Description = "Tijolos de solo-cimento disponíveis para retirada no local.",
                    Price = 1.25m,
                    Unit = "unidade",
                    Quantity = 800m,
                    Condition = MaterialCondition.Good,
                    Status = ListingStatus.Active,
                    City = "Navegantes",
                    State = "SC",
                    SellerDisplayName = "Obra residencial",
                    CreatedAtUtc = SeedIds.CreatedAtUtc
                });
        });
    }
}

internal static class SeedIds
{
    public static readonly Guid Revestimentos = Guid.Parse("9473e2aa-0fa8-4e01-b2cf-99781af54c01");
    public static readonly Guid Madeira = Guid.Parse("9473e2aa-0fa8-4e01-b2cf-99781af54c02");
    public static readonly Guid Hidraulica = Guid.Parse("9473e2aa-0fa8-4e01-b2cf-99781af54c03");
    public static readonly Guid Eletrica = Guid.Parse("9473e2aa-0fa8-4e01-b2cf-99781af54c04");
    public static readonly Guid Ferragens = Guid.Parse("9473e2aa-0fa8-4e01-b2cf-99781af54c05");
    public static readonly Guid Porcelanato = Guid.Parse("a304bbca-6477-4490-957b-10bc19e7ca01");
    public static readonly Guid Portas = Guid.Parse("a304bbca-6477-4490-957b-10bc19e7ca02");
    public static readonly Guid Tijolos = Guid.Parse("a304bbca-6477-4490-957b-10bc19e7ca03");
    public static readonly DateTime CreatedAtUtc = new(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc);
}
