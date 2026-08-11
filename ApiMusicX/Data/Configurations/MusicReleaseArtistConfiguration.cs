using ApiMusicX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiMusicX.Data.Configurations;

public sealed class MusicReleaseArtistConfiguration : IEntityTypeConfiguration<MusicReleaseArtist>
{
    public void Configure(EntityTypeBuilder<MusicReleaseArtist> builder)
    {
        builder.ToTable("MusicReleaseArtists", table => table.HasCheckConstraint(
            "CK_MusicReleaseArtists_Role",
            "[Role] IN ('Unknown', 'Primary', 'Featured', 'Composer')"));
        builder.HasKey(credit => new { credit.MusicReleaseId, credit.MusicArtistId });
        builder.Property(credit => credit.Role).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.HasOne(credit => credit.MusicRelease)
            .WithMany(release => release.ArtistCredits)
            .HasForeignKey(credit => credit.MusicReleaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(credit => credit.MusicArtist)
            .WithMany(artist => artist.ReleaseCredits)
            .HasForeignKey(credit => credit.MusicArtistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
