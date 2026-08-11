using ApiMusicX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiMusicX.Data.Configurations;

public sealed class MusicArtistAliasConfiguration : IEntityTypeConfiguration<MusicArtistAlias>
{
    public void Configure(EntityTypeBuilder<MusicArtistAlias> builder)
    {
        builder.ToTable("MusicArtistAliases");
        builder.HasKey(alias => alias.MusicArtistAliasId);
        builder.Property(alias => alias.MusicArtistAliasId).ValueGeneratedOnAdd();
        builder.Property(alias => alias.Value).IsRequired().HasMaxLength(256);
        builder.Property(alias => alias.NormalizedValue).IsRequired().HasMaxLength(256);
        builder.HasIndex(alias => new { alias.MusicArtistId, alias.NormalizedValue }).IsUnique();
        builder.HasOne(alias => alias.MusicArtist)
            .WithMany(artist => artist.Aliases)
            .HasForeignKey(alias => alias.MusicArtistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
