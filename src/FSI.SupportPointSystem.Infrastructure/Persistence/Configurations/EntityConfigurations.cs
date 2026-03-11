using FSI.SupportPointSystem.Domain.Entities;
using FSI.SupportPointSystem.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSI.SupportPointSystem.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Cpf)
            .HasConversion(
                v => v.Value,
                v => Cpf.Create(v))
            .HasColumnName("Cpf")
            .HasMaxLength(11)
            .IsRequired();

        builder.HasIndex(u => u.Cpf).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt);

        builder.HasOne<Seller>() 
            .WithOne(s => s.User)
            .HasForeignKey<Seller>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SellerConfiguration : IEntityTypeConfiguration<Seller>
{
    public void Configure(EntityTypeBuilder<Seller> builder)
    {
        builder.ToTable("Sellers");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Phone).HasMaxLength(20);
        builder.Property(s => s.Email).HasMaxLength(150);
        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt);
    }
}

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CompanyName).HasMaxLength(150).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();

        builder.Property(c => c.Cnpj)
            .HasConversion(
                v => v.Value,
                v => Cnpj.Create(v))
            .HasColumnName("Cnpj")
            .HasMaxLength(14)
            .IsRequired();

        builder.OwnsOne(c => c.LocationTarget, coords =>
        {
            coords.Property(x => x.Latitude)
                .HasColumnName("LatitudeTarget")
                .HasPrecision(12, 9)
                .IsRequired();
            coords.Property(x => x.Longitude)
                .HasColumnName("LongitudeTarget")
                .HasPrecision(12, 9)
                .IsRequired();
        });

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt);
    }
}

public sealed class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable("Checkins");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.SellerId).IsRequired();
        builder.Property(v => v.CustomerId).IsRequired();
        builder.Property(v => v.CheckinDistanceMeters).IsRequired();
        builder.Property(v => v.CheckinTimestamp).IsRequired();
        builder.Property(v => v.DurationMinutes);
        builder.Property(v => v.CheckoutSummary).HasMaxLength(500);

        builder.OwnsOne(v => v.CheckinLocation, coords =>
        {
            coords.Property(x => x.Latitude)
                .HasColumnName("CheckinLatitude")
                .HasPrecision(12, 9)
                .IsRequired();
            coords.Property(x => x.Longitude)
                .HasColumnName("CheckinLongitude")
                .HasPrecision(12, 9)
                .IsRequired();
        });

        builder.OwnsOne(v => v.CheckoutLocation, coords =>
        {
            coords.Property(x => x.Latitude)
                .HasColumnName("CheckoutLatitude")
                .HasPrecision(12, 9);
            coords.Property(x => x.Longitude)
                .HasColumnName("CheckoutLongitude")
                .HasPrecision(12, 9);
        });

        // CORREÇÃO PARA MYSQL: Filtros de índice não usam colchetes []
        builder.HasIndex(v => new { v.SellerId, v.CheckoutTimestamp })
            .HasFilter("`CheckoutTimestamp` IS NULL")
            .HasDatabaseName("IX_Visits_Seller_ActiveOnly");

        builder.HasOne<Seller>()
            .WithMany()
            .HasForeignKey(v => v.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(v => v.CreatedAt).IsRequired();
        builder.Property(v => v.UpdatedAt);
    }
}