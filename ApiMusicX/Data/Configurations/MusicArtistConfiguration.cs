using ApiMusicX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiMusicX.Data.Configurations;

public sealed class MusicArtistConfiguration : IEntityTypeConfiguration<MusicArtist>
{
    public void Configure(EntityTypeBuilder<MusicArtist> builder)
    {
        builder.ToTable("MusicArtists", table => table.HasCheckConstraint(
            "CK_MusicArtists_ArtistType",
            "[ArtistType] IN ('Unknown', 'Solo', 'Band', 'Group')"));
        builder.HasKey(artist => artist.MusicArtistId);
        builder.Property(artist => artist.MusicArtistId).ValueGeneratedOnAdd();
        builder.Property(artist => artist.DisplayName).IsRequired().HasMaxLength(256);
        builder.Property(artist => artist.NormalizedName).IsRequired().HasMaxLength(256);
        builder.Property(artist => artist.ArtistType).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(artist => artist.SortName).HasMaxLength(256);
        builder.HasIndex(artist => artist.NormalizedName);
    }
}
