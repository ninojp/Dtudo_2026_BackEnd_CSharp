using ApiMusicX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiMusicX.Data.Configurations;

public sealed class MusicReleaseConfiguration : IEntityTypeConfiguration<MusicRelease>
{
    public void Configure(EntityTypeBuilder<MusicRelease> builder)
    {
        builder.ToTable("MusicReleases", table => table.HasCheckConstraint(
            "CK_MusicReleases_ReleaseType",
            "[ReleaseType] IN ('Unknown', 'Album', 'Single', 'EP', 'Compilation', 'Video')"));
        builder.HasKey(release => release.MusicReleaseId);
        builder.Property(release => release.MusicReleaseId).ValueGeneratedOnAdd();
        builder.Property(release => release.Title).IsRequired().HasMaxLength(512);
        builder.Property(release => release.NormalizedTitle).IsRequired().HasMaxLength(512);
        builder.Property(release => release.ReleaseType).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(release => release.ReleaseYear);
        builder.Property(release => release.Notes).HasMaxLength(2000);
        builder.HasIndex(release => release.NormalizedTitle);
        builder.ToTable("MusicReleases", table => table.HasCheckConstraint(
            "CK_MusicReleases_ReleaseYear",
            "[ReleaseYear] IS NULL OR [ReleaseYear] BETWEEN 1000 AND 9999"));
    }
}
