using ApiMusicX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiMusicX.Data.Configurations;

public sealed class MusicTrackConfiguration : IEntityTypeConfiguration<MusicTrack>
{
    public void Configure(EntityTypeBuilder<MusicTrack> builder)
    {
        builder.ToTable("MusicTracks", table => table.HasCheckConstraint(
            "CK_MusicTracks_Durations",
            "([DurationSeconds] IS NULL OR [DurationSeconds] >= 0) AND ([Sequence] IS NULL OR [Sequence] >= 0)"));
        builder.HasKey(track => track.MusicTrackId);
        builder.Property(track => track.MusicTrackId).ValueGeneratedOnAdd();
        builder.Property(track => track.MusicReleaseId).IsRequired();
        builder.Property(track => track.PositionLabel).HasMaxLength(64);
        builder.Property(track => track.Sequence);
        builder.Property(track => track.Title).IsRequired().HasMaxLength(512);
        builder.Property(track => track.NormalizedTitle).IsRequired().HasMaxLength(512);
        builder.Property(track => track.DurationSeconds);
        builder.Property(track => track.DurationText).HasMaxLength(32);
        builder.Property(track => track.Notes).HasMaxLength(2000);
        builder.HasIndex(track => new { track.MusicReleaseId, track.PositionLabel, track.NormalizedTitle })
            .IsUnique()
            .HasFilter("[PositionLabel] IS NOT NULL");
        builder.HasIndex(track => new { track.MusicReleaseId, track.Sequence, track.NormalizedTitle })
            .IsUnique()
            .HasFilter("[Sequence] IS NOT NULL");
        builder.HasOne(track => track.MusicRelease)
            .WithMany(release => release.Tracks)
            .HasForeignKey(track => track.MusicReleaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
