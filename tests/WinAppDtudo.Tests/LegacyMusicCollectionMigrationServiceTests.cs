using System.Net;
using System.Text;
using WinAppDtudo.Services;

namespace WinAppDtudo.Tests;

public sealed class LegacyMusicCollectionMigrationServiceTests
{
    [Fact]
    public async Task ImportaRegistroCompletoComDiscogsEReferenciaLocal()
    {
        var json = """
        {
          "mymusicx": [
            {
              "id": "banda-a",
              "artista": "Banda A",
              "releases": {
                "albums": [
                  {
                    "discogs_id": "12345",
                    "titulo": "Album A",
                    "ano": "2020",
                    "arquivosLocais": ["Album A/01 - Faixa.mp3", "Album A/Capa.jpg"]
                  }
                ],
                "singles-EP": [],
                "compilations": [],
                "videos": []
              }
            }
          ]
        }
        """;
        var importer = new RecordingImporter();
        var service = new LegacyMusicCollectionMigrationService(importer);
        var path = await WriteJsonAsync(json);

        try
        {
            var result = await service.ExecutarAsync(path, dryRun: false);
            var request = Assert.Single(importer.Requests);
            var release = Assert.Single(request.Releases);

            Assert.Equal(1, result.Summary.Lidos);
            Assert.Equal(1, result.Summary.Importados);
            Assert.Equal(0, result.Summary.Falhos);
            Assert.Equal("banda-a", request.ExternalIdentifiers.Single().ExternalId);
            Assert.Equal("12345", release.ExternalIdentifiers.Single().ExternalId);
            Assert.Equal(2020, release.ReleaseYear);
            Assert.Equal(2, release.LocalFileReferences.Count);
            Assert.Equal(ApiMusicXMediaKind.Audio, release.LocalFileReferences[0].MediaKind);
            Assert.Equal(ApiMusicXMediaKind.Image, release.LocalFileReferences[1].MediaKind);
            Assert.Equal("Album A/01 - Faixa.mp3", release.LocalFileReferences[0].RelativePath);
        }
        finally
        {
            DeleteTemporaryFile(path);
        }
    }

    [Fact]
    public async Task RegistroSemIdDiscogsMantemColecaoEReleaseSemIdentificadorExterno()
    {
        var json = CreateJson(
            "sem-discogs",
            "Artista Sem Discogs",
          "",
          "Release local",
          "");
        var service = new LegacyMusicCollectionMigrationService();
        var path = await WriteJsonAsync(json);

        try
        {
            var result = await service.ExecutarAsync(path, dryRun: true);
            var item = Assert.Single(result.Items);
            Assert.NotNull(item.Request);
            var request = item.Request!;
            var release = Assert.Single(request.Releases);

            Assert.Equal(1, result.Summary.Simulados);
            Assert.Empty(release.ExternalIdentifiers);
            Assert.Null(release.ReleaseYear);
            Assert.Equal("sem-discogs", request.ExternalIdentifiers.Single().ExternalId);
        }
        finally
        {
            DeleteTemporaryFile(path);
        }
    }

    [Fact]
    public async Task DryRunMapeiaReferenciaLocalSemVerificarOuCriarArquivoDeMusica()
    {
        var json = CreateJson(
            "referencia-local",
            "Artista Local",
          "77",
          "Colecao com arquivo",
          "2021",
          "Musicas/arquivo.flac");
        var service = new LegacyMusicCollectionMigrationService();
        var path = await WriteJsonAsync(json);
        var musicPath = Path.Combine(Path.GetDirectoryName(path)!, "Musicas", "arquivo.flac");

        try
        {
            var result = await service.ExecutarAsync(path, dryRun: true);
            var reference = Assert.Single(Assert.Single(Assert.Single(result.Items).Request!.Releases).LocalFileReferences);

            Assert.Equal("Musicas/arquivo.flac", reference.RelativePath);
            Assert.False(File.Exists(musicPath));
            Assert.Equal(1, result.Summary.Simulados);
        }
        finally
        {
            DeleteTemporaryFile(path);
            if (Directory.Exists(Path.GetDirectoryName(musicPath)))
            {
                Directory.Delete(Path.GetDirectoryName(musicPath)!, recursive: true);
            }
        }
    }

    [Fact]
    public async Task IgnoraRegistroDuplicadoEReleaseDuplicadoDeFormaDeterministica()
    {
        var json = """
        {
          "mymusicx": [
            {
              "id": "duplicado",
              "artista": "Artista Duplicado",
              "releases": {
                "albums": [
                  { "discogs_id": "1", "titulo": "Mesmo Release", "ano": "2020", "arquivosLocais": [] },
                  { "discogs_id": "1", "titulo": "Mesmo Release", "ano": "2020", "arquivosLocais": [] }
                ]
              }
            },
            {
              "id": "duplicado",
              "artista": "Artista Duplicado",
              "releases": { "albums": [] }
            }
          ]
        }
        """;
        var service = new LegacyMusicCollectionMigrationService();
        var path = await WriteJsonAsync(json);

        try
        {
            var result = await service.ExecutarAsync(path, dryRun: true);
            var firstItem = result.Items[0];
            var secondItem = result.Items[1];
            Assert.NotNull(firstItem.Request);

            Assert.Equal(2, result.Summary.Lidos);
            Assert.Equal(1, result.Summary.Simulados);
            Assert.Equal(1, result.Summary.Ignorados);
            Assert.True(secondItem.IsDuplicate);
            Assert.Single(firstItem.Request!.Releases);
            Assert.Contains(firstItem.Warnings, warning => warning.Contains("duplicado", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTemporaryFile(path);
        }
    }

    [Fact]
    public async Task MapeiaTracksQuandoOFormatoLegadoAsFornece()
    {
        var json = """
        {
          "mymusicx": [
            {
              "id": "com-tracks",
              "artista": "Artista com Tracks",
              "releases": {
                "albums": [
                  {
                    "discogs_id": "8",
                    "titulo": "Album com Tracks",
                    "tracks": [
                      {
                        "titulo": "Faixa 1",
                        "posicao": "A1",
                        "sequencia": 1,
                        "arquivosLocais": ["Album/01.mp3"]
                      }
                    ]
                  }
                ]
              }
            }
          ]
        }
        """;
        var service = new LegacyMusicCollectionMigrationService();
        var path = await WriteJsonAsync(json);

        try
        {
            var result = await service.ExecutarAsync(path, dryRun: true);
            var track = Assert.Single(Assert.Single(Assert.Single(result.Items).Request!.Releases).Tracks);

            Assert.Equal("Faixa 1", track.Title);
            Assert.Equal("A1", track.PositionLabel);
            Assert.Equal(1, track.Sequence);
            Assert.Equal("Album/01.mp3", Assert.Single(track.LocalFileReferences).RelativePath);
        }
        finally
        {
            DeleteTemporaryFile(path);
        }
    }

    [Fact]
    public async Task TrataAnoInvalidoSemEnviarValorInvalidoParaApi()
    {
        var json = CreateJson(
            "ano-invalido",
            "Artista Ano",
          "9",
          "Release com ano invalido",
          "19xx");
        var service = new LegacyMusicCollectionMigrationService();
        var path = await WriteJsonAsync(json);

        try
        {
            var result = await service.ExecutarAsync(path, dryRun: true);
            var item = Assert.Single(result.Items);
            Assert.NotNull(item.Request);
            var release = Assert.Single(item.Request!.Releases);

            Assert.Null(release.ReleaseYear);
            Assert.Contains(item.Warnings, warning => warning.Contains("Ano invalido", StringComparison.Ordinal));
            Assert.Equal(1, result.Summary.Simulados);
            Assert.Equal(0, result.Summary.Falhos);
        }
        finally
        {
            DeleteTemporaryFile(path);
        }
    }

    [Fact]
    public async Task RejeitaBytesQueNaoSaoUtf8Valido()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dtudo-legacy-{Guid.NewGuid():N}.json");
        await File.WriteAllBytesAsync(path, [0x7B, 0x22, 0xFF, 0x22, 0x3A, 0x31, 0x7D]);
        var service = new LegacyMusicCollectionMigrationService();

        try
        {
            var exception = await Assert.ThrowsAsync<LegacyMusicMigrationException>(() =>
                service.ExecutarAsync(path, dryRun: true));

            Assert.Contains("UTF-8", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTemporaryFile(path);
        }
    }

      [Fact]
      public async Task DryRunDoJsonLegadoRealNaoGeraFalhasDeNormalizacao()
      {
        var repositoryRoot = FindRepositoryRoot();
        var path = Path.Combine(repositoryRoot, "ApiNode", "mymusicx", "mymusicx.json");
        Assert.True(File.Exists(path), $"JSON legado nao encontrado: {path}");
        var service = new LegacyMusicCollectionMigrationService();

        var result = await service.ExecutarAsync(path, dryRun: true);

        Assert.Equal(85, result.Summary.Lidos);
        Assert.Equal(85, result.Summary.Simulados);
        Assert.Equal(0, result.Summary.Falhos);
        Assert.True(result.Items.SelectMany(item => item.Request?.Releases ?? []).Any());
        Assert.True(result.Items.SelectMany(item => item.Request?.Releases ?? [])
          .SelectMany(release => release.LocalFileReferences)
          .Any());
      }

    private static string CreateJson(
        string id,
        string artist,
        string discogs_id,
        string titulo,
        string ano,
        string? arquivosLocais = null)
    {
        var files = arquivosLocais is null ? "[]" : $"[\"{arquivosLocais}\"]";
        return $$"""
        {
          "mymusicx": [
            {
              "id": "{{id}}",
              "artista": "{{artist}}",
              "releases": {
                "albums": [
                  {
                    "discogs_id": "{{discogs_id}}",
                    "titulo": "{{titulo}}",
                    "ano": "{{ano}}",
                    "arquivosLocais": {{files}}
                  }
                ]
              }
            }
          ]
        }
        """;
    }

    private static async Task<string> WriteJsonAsync(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dtudo-legacy-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json, new UTF8Encoding(false));
        return path;
    }

    private static void DeleteTemporaryFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

      private static string FindRepositoryRoot()
      {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
          if (File.Exists(Path.Combine(directory.FullName, "Dtudo2026.slnx")))
          {
            return directory.FullName;
          }

          directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("A raiz do repositorio Dtudo2026 nao foi encontrada.");
      }

    private sealed class RecordingImporter : IApiMusicXCollectionImporter
    {
        public List<ApiMusicXImportCollectionRequest> Requests { get; } = [];

        public Task<ApiMusicXImportCollectionResponse> ImportarColecaoAsync(
            ApiMusicXImportCollectionRequest request,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(new ApiMusicXImportCollectionResponse(
                new ApiMusicXCollectionDto(
                    1,
                    request.DisplayName,
                    request.Description,
                    [],
                    [],
                    request.ExternalIdentifiers
                        .Select(identifier => new ApiMusicXExternalIdentifierDto(
                            identifier.Provider,
                            identifier.ResourceType,
                            identifier.ExternalId))
                        .ToList()),
                Created: true,
                Changed: true,
                ArtistsAdded: request.Artists.Count,
                ReleasesAdded: request.Releases.Count,
                TracksAdded: request.Releases.Sum(release => release.Tracks.Count),
                LocalFileReferencesAdded: request.Releases.Sum(release =>
                    release.LocalFileReferences.Count + release.Tracks.Sum(track => track.LocalFileReferences.Count))));
        }
    }
}
