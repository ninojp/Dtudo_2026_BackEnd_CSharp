using ApiMusicX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiMusicX.Data.Configurations;

public sealed class MusicCollectionConfiguration : IEntityTypeConfiguration<MusicCollection>
{
    public void Configure(EntityTypeBuilder<MusicCollection> builder)
    {
        builder.ToTable("MusicCollections");
        builder.HasKey(collection => collection.MusicCollectionId);
        builder.Property(collection => collection.MusicCollectionId).ValueGeneratedOnAdd();
        builder.Property(collection => collection.DisplayName).IsRequired().HasMaxLength(256);
        builder.Property(collection => collection.NormalizedName).IsRequired().HasMaxLength(256);
        builder.Property(collection => collection.Description).HasMaxLength(2000);
        builder.HasIndex(collection => collection.NormalizedName);
    }
}
