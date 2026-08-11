using ApiMusicX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiMusicX.Data.Configurations;

public sealed class ExternalSourceIdentifierConfiguration : IEntityTypeConfiguration<ExternalSourceIdentifier>
{
    public void Configure(EntityTypeBuilder<ExternalSourceIdentifier> builder)
    {
        builder.ToTable("ExternalSourceIdentifiers", table => table.HasCheckConstraint(
            "CK_ExternalSourceIdentifiers_ExactlyOneOwner",
            "(CASE WHEN [MusicArtistId] IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN [MusicCollectionId] IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN [MusicReleaseId] IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN [MusicTrackId] IS NOT NULL THEN 1 ELSE 0 END) = 1"));
        builder.HasKey(identifier => identifier.ExternalSourceIdentifierId);
        builder.Property(identifier => identifier.ExternalSourceIdentifierId).ValueGeneratedOnAdd();
        builder.Property(identifier => identifier.Provider).IsRequired().HasMaxLength(64);
        builder.Property(identifier => identifier.ResourceType).IsRequired().HasMaxLength(64);
        builder.Property(identifier => identifier.ExternalId).IsRequired().HasMaxLength(256);
        builder.HasIndex(identifier => new
        {
            identifier.Provider,
            identifier.ResourceType,
            identifier.ExternalId
        }).IsUnique();

        builder.HasOne(identifier => identifier.MusicArtist)
            .WithMany(artist => artist.ExternalIdentifiers)
            .HasForeignKey(identifier => identifier.MusicArtistId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(identifier => identifier.MusicCollection)
            .WithMany(collection => collection.ExternalIdentifiers)
            .HasForeignKey(identifier => identifier.MusicCollectionId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(identifier => identifier.MusicRelease)
            .WithMany(release => release.ExternalIdentifiers)
            .HasForeignKey(identifier => identifier.MusicReleaseId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(identifier => identifier.MusicTrack)
            .WithMany(track => track.ExternalIdentifiers)
            .HasForeignKey(identifier => identifier.MusicTrackId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
