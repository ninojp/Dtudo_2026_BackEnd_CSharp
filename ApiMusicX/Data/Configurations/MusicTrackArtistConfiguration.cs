using ApiMusicX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiMusicX.Data.Configurations;

public sealed class MusicTrackArtistConfiguration : IEntityTypeConfiguration<MusicTrackArtist>
{
    public void Configure(EntityTypeBuilder<MusicTrackArtist> builder)
    {
        builder.ToTable("MusicTrackArtists", table => table.HasCheckConstraint(
            "CK_MusicTrackArtists_Role",
            "[Role] IN ('Unknown', 'Primary', 'Featured', 'Composer')"));
        builder.HasKey(credit => new { credit.MusicTrackId, credit.MusicArtistId });
        builder.Property(credit => credit.Role).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.HasOne(credit => credit.MusicTrack)
            .WithMany(track => track.ArtistCredits)
            .HasForeignKey(credit => credit.MusicTrackId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(credit => credit.MusicArtist)
            .WithMany(artist => artist.TrackCredits)
            .HasForeignKey(credit => credit.MusicArtistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}