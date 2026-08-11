using ApiMusicX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiMusicX.Data.Configurations;

public sealed class MusicLocalFileReferenceConfiguration : IEntityTypeConfiguration<MusicLocalFileReference>
{
    public void Configure(EntityTypeBuilder<MusicLocalFileReference> builder)
    {
        builder.ToTable("MusicLocalFileReferences", table => table.HasCheckConstraint(
            "CK_MusicLocalFileReferences_RelativePath",
            "[NormalizedPath] <> '' AND [NormalizedPath] NOT LIKE '/%' AND [NormalizedPath] NOT LIKE '\\\\%' AND [NormalizedPath] NOT LIKE '[A-Za-z]:%' AND [NormalizedPath] NOT LIKE '%..%'"));
        builder.HasKey(reference => reference.MusicLocalFileReferenceId);
        builder.Property(reference => reference.MusicLocalFileReferenceId).ValueGeneratedOnAdd();
        builder.Property(reference => reference.MusicReleaseId).IsRequired();
        builder.Property(reference => reference.MusicTrackId);
        builder.Property(reference => reference.RelativePath).IsRequired().HasMaxLength(1024);
        builder.Property(reference => reference.NormalizedPath).IsRequired().HasMaxLength(1024);
        builder.Property(reference => reference.MediaKind).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(reference => reference.Role).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(reference => new { reference.MusicReleaseId, reference.NormalizedPath })
            .IsUnique()
            .HasFilter("[MusicTrackId] IS NULL");
        builder.HasIndex(reference => new { reference.MusicTrackId, reference.NormalizedPath })
            .IsUnique()
            .HasFilter("[MusicTrackId] IS NOT NULL");
        builder.HasOne(reference => reference.MusicRelease)
            .WithMany(release => release.LocalFileReferences)
            .HasForeignKey(reference => reference.MusicReleaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(reference => reference.MusicTrack)
            .WithMany(track => track.LocalFileReferences)
            .HasForeignKey(reference => reference.MusicTrackId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
