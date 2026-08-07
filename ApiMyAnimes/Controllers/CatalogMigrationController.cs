using ApiMyAnimes.Data;
using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiMyAnimes.Controllers;

/// <summary>
/// Expõe comandos pequenos e repetiveis para a migracao do cliente administrativo.
/// Estes comandos nao transferem o acesso ao banco para o cliente.
/// </summary>
/// <param name="context">Contexto proprietario do banco local de catalogo.</param>
[ApiController]
[Route("apiLocal/catalog-migration")]
[Authorize]
public sealed class CatalogMigrationController(MyAnimesContext context) : ControllerBase
{
    /// <summary>
    /// Garante uma colecao pelo titulo normalizado e mescla os MalIds informados sem duplicidade.
    /// Repetir o mesmo PUT preserva o mesmo recurso e nao cria uma nova colecao.
    /// </summary>
    [HttpPut("my-animes/by-title")]
    [Authorize(Policy = "permission:catalog.write")]
    [ProducesResponseType(typeof(EnsureMyAnimeCollectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EnsureMyAnimeCollectionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<EnsureMyAnimeCollectionResponse> EnsureMyAnimeCollection(
        [FromBody] EnsureMyAnimeCollectionRequest? request)
    {
        if (request is null || request.AnimesMalId is null)
            return BadRequest("Corpo da requisicao invalido.");

        var title = request.Titulo.Trim();
        var malIds = request.AnimesMalId
            .Where(malId => malId > 0)
            .Distinct()
            .OrderBy(malId => malId)
            .ToList();

        if (string.IsNullOrWhiteSpace(title) || malIds.Count == 0)
            return BadRequest("Titulo e pelo menos um MalId positivo sao obrigatorios.");

        var collection = context.MyAnimes.FirstOrDefault(item =>
            item.Titulo.Trim().ToLower() == title.ToLower());
        var created = collection is null;
        var changed = false;

        if (collection is null)
        {
            collection = new MyAnime
            {
                Titulo = title,
                AnimesMalId = malIds
            };
            context.MyAnimes.Add(collection);
            changed = true;
        }
        else
        {
            var mergedMalIds = collection.AnimesMalId
                .Concat(malIds)
                .Where(malId => malId > 0)
                .Distinct()
                .OrderBy(malId => malId)
                .ToList();

            changed = !collection.AnimesMalId.SequenceEqual(mergedMalIds);
            if (changed)
                collection.AnimesMalId = mergedMalIds;
        }

        if (changed)
            context.SaveChanges();

        var response = new EnsureMyAnimeCollectionResponse
        {
            Id = collection.Id,
            Titulo = collection.Titulo,
            AnimesMalId = collection.AnimesMalId,
            Created = created,
            Changed = changed
        };

        return created
            ? StatusCode(StatusCodes.Status201Created, response)
            : Ok(response);
    }

    /// <summary>
    /// Garante a associacao entre um anime e uma colecao local.
    /// O comando atualiza somente o vinculo e a lista de MalIds da colecao.
    /// </summary>
    [HttpPut("animes/{malId:int}/my-anime")]
    [Authorize(Policy = "permission:catalog.write")]
    [ProducesResponseType(typeof(EnsureAnimeAssociationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<EnsureAnimeAssociationResponse> EnsureAnimeAssociation(
        int malId,
        [FromBody] EnsureAnimeAssociationRequest? request)
    {
        if (malId <= 0 || request is null || request.MyAnimeId <= 0)
            return BadRequest("MalId e MyAnimeId devem ser numeros positivos.");

        var collection = context.MyAnimes.FirstOrDefault(item => item.Id == request.MyAnimeId);
        if (collection is null)
            return NotFound($"Colecao MyAnime com ID {request.MyAnimeId} nao encontrada.");

        var anime = context.Animes.FirstOrDefault(item => item.MalId == malId);
        if (anime is null)
            return NotFound($"Anime com MalId {malId} nao encontrado.");

        var previousMyAnimeId = anime.MyAnimeID;
        var previousCollection = previousMyAnimeId > 0 && previousMyAnimeId != request.MyAnimeId
            ? context.MyAnimes.FirstOrDefault(item => item.Id == previousMyAnimeId)
            : null;

        var targetMalIds = collection.AnimesMalId
            .Append(malId)
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList();
        var targetChanged = !collection.AnimesMalId.SequenceEqual(targetMalIds);
        if (targetChanged)
        {
            collection.AnimesMalId = targetMalIds;
        }

        var previousChanged = false;
        if (previousCollection is not null)
        {
            var previousMalIds = previousCollection.AnimesMalId
                .Where(id => id > 0 && id != malId)
                .Distinct()
                .OrderBy(id => id)
                .ToList();
            previousChanged = !previousCollection.AnimesMalId.SequenceEqual(previousMalIds);
            if (previousChanged)
            {
                previousCollection.AnimesMalId = previousMalIds;
            }
        }

        anime.MyAnimeID = request.MyAnimeId;
        var changed = previousMyAnimeId != request.MyAnimeId || targetChanged || previousChanged;
        if (changed)
            context.SaveChanges();

        return Ok(new EnsureAnimeAssociationResponse
        {
            MalId = malId,
            MyAnimeId = request.MyAnimeId,
            Changed = changed
        });
    }
}
