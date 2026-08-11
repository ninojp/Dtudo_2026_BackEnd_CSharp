using ApiMusicX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiMusicX.Data.Configurations;

public sealed class MusicCollectionArtistConfiguration : IEntityTypeConfiguration<MusicCollectionArtist>
{
    public void Configure(EntityTypeBuilder<MusicCollectionArtist> builder)
    {
        builder.ToTable("MusicCollectionArtists", table => table.HasCheckConstraint(
            "CK_MusicCollectionArtists_Role",
            "[Role] IN ('Unknown', 'Primary', 'Member', 'Associated')"));
        builder.HasKey(link => new { link.MusicCollectionId, link.MusicArtistId });
        builder.Property(link => link.Role).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.HasOne(link => link.MusicCollection)
            .WithMany(collection => collection.ArtistLinks)
            .HasForeignKey(link => link.MusicCollectionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(link => link.MusicArtist)
            .WithMany(artist => artist.CollectionLinks)
            .HasForeignKey(link => link.MusicArtistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
