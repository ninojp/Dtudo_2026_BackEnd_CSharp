using ApiMusicX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiMusicX.Data.Configurations;

public sealed class MusicCollectionReleaseConfiguration : IEntityTypeConfiguration<MusicCollectionRelease>
{
    public void Configure(EntityTypeBuilder<MusicCollectionRelease> builder)
    {
        builder.ToTable("MusicCollectionReleases", table => table.HasCheckConstraint(
            "CK_MusicCollectionReleases_DisplayOrder",
            "[DisplayOrder] IS NULL OR [DisplayOrder] >= 0"));
        builder.HasKey(link => new { link.MusicCollectionId, link.MusicReleaseId });
        builder.Property(link => link.SourceCategory).HasMaxLength(64);
        builder.Property(link => link.DisplayOrder);
        builder.HasOne(link => link.MusicCollection)
            .WithMany(collection => collection.ReleaseLinks)
            .HasForeignKey(link => link.MusicCollectionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(link => link.MusicRelease)
            .WithMany(release => release.CollectionLinks)
            .HasForeignKey(link => link.MusicReleaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
