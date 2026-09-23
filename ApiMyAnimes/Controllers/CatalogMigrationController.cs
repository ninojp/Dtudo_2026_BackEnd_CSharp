using ApiMyAnimes.Data;
using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    /// Garante uma colecao pelo titulo normalizado e mescla somente os MalIds livres, sem duplicidade.
    /// MalIds pertencentes a outra colecao sao preservados na colecao original.
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
        var malIdsIgnorados = ObterMalIdsPertencentesAOutraColecao(collection?.Id, malIds);
        var malIdsDisponiveis = malIds
            .Where(malId => !malIdsIgnorados.Contains(malId))
            .ToList();

        if (collection is null && malIdsDisponiveis.Count == 0)
            return Conflict("Todos os MalIds informados já pertencem a outras coleções.");

        var created = collection is null;
        var changed = false;

        if (collection is null)
        {
            collection = new MyAnime
            {
                Titulo = title,
                AnimesMalId = malIdsDisponiveis
            };
            context.MyAnimes.Add(collection);
            changed = true;
        }
        else
        {
            var mergedMalIds = collection.AnimesMalId
                .Where(malId => malId > 0 && !malIdsIgnorados.Contains(malId))
                .Concat(malIdsDisponiveis)
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
            AnimesIgnoradosPorOutraColecao = malIdsIgnorados.OrderBy(malId => malId).ToList(),
            Created = created,
            Changed = changed
        };

        return created
            ? StatusCode(StatusCodes.Status201Created, response)
            : Ok(response);
    }

    /// <summary>
    /// Garante a associacao entre um anime e uma colecao local.
    /// O comando atualiza somente o vinculo e a lista de MalIds da colecao;
    /// nunca move um anime que ja pertence a outra colecao.
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

        var myAnimeIdPorLista = ObterOutraColecaoQueContemMalId(malId, request.MyAnimeId);
        var anime = context.Animes.FirstOrDefault(item => item.MalId == malId);
        if (anime is null && !myAnimeIdPorLista.HasValue)
            return NotFound($"Anime com MalId {malId} nao encontrado.");

        var myAnimeIdAtual = anime is not null && anime.MyAnimeID > 0 && anime.MyAnimeID != request.MyAnimeId
            ? anime.MyAnimeID
            : anime is null || anime.MyAnimeID <= 0
                ? myAnimeIdPorLista
                : null;

        if (myAnimeIdAtual.HasValue)
        {
            var targetMalIdsSemConflito = collection.AnimesMalId
                .Where(id => id > 0 && id != malId)
                .Distinct()
                .OrderBy(id => id)
                .ToList();
            var targetConflictChanged = !collection.AnimesMalId.SequenceEqual(targetMalIdsSemConflito);
            if (targetConflictChanged)
            {
                collection.AnimesMalId = targetMalIdsSemConflito;
                context.SaveChanges();
            }

            return Ok(new EnsureAnimeAssociationResponse
            {
                MalId = malId,
                MyAnimeId = request.MyAnimeId,
                MyAnimeIdAtual = myAnimeIdAtual,
                IgnoradaPorOutraColecao = true,
                Changed = targetConflictChanged
            });
        }

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

        var animeChanged = anime!.MyAnimeID != request.MyAnimeId;
        anime.MyAnimeID = request.MyAnimeId;
        var changed = animeChanged || targetChanged;
        if (changed)
            context.SaveChanges();

        return Ok(new EnsureAnimeAssociationResponse
        {
            MalId = malId,
            MyAnimeId = request.MyAnimeId,
            MyAnimeIdAtual = request.MyAnimeId,
            Changed = changed
        });
    }

    /// <summary>
    /// Identifica e, quando solicitado explicitamente, limpa referências de Anime para
    /// coleções MyAnime que já não existem. A simulação é o comportamento padrão.
    /// </summary>
    [HttpPost("animes/orphan-associations/repair")]
    [Authorize(Policy = "permission:catalog.write")]
    [ProducesResponseType(typeof(RepararAssociacoesOrfasResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<RepararAssociacoesOrfasResponse> RepararAssociacoesOrfas(
        [FromBody] RepararAssociacoesOrfasRequest? request)
    {
        if (request is null || request.MyAnimeIds is null)
            return BadRequest("Corpo da requisicao invalido.");

        var myAnimeIds = request.MyAnimeIds
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList();
        if (myAnimeIds.Count == 0)
            return BadRequest("Informe pelo menos um MyAnimeId positivo.");

        var colecaoIdsExistentes = context.MyAnimes
            .AsNoTracking()
            .Where(colecao => myAnimeIds.Contains(colecao.Id))
            .Select(colecao => colecao.Id)
            .ToHashSet();
        var myAnimeIdsSemColecao = myAnimeIds
            .Where(id => !colecaoIdsExistentes.Contains(id))
            .ToList();
        var animesOrfaos = context.Animes
            .Where(anime => myAnimeIdsSemColecao.Contains(anime.MyAnimeID))
            .OrderBy(anime => anime.MalId)
            .ToList();
        var malIdsEncontrados = animesOrfaos
            .Select(anime => anime.MalId)
            .ToList();

        var malIdsCorrigidos = new List<int>();
        if (request.Aplicar && animesOrfaos.Count > 0)
        {
            foreach (var anime in animesOrfaos)
            {
                anime.MyAnimeID = 0;
                malIdsCorrigidos.Add(anime.MalId);
            }

            context.SaveChanges();
        }

        return Ok(new RepararAssociacoesOrfasResponse
        {
            Simulacao = !request.Aplicar,
            MyAnimeIdsSolicitados = myAnimeIds,
            MyAnimeIdsSemColecao = myAnimeIdsSemColecao,
            MyAnimeIdsComColecaoPreservados = myAnimeIds
                .Where(colecaoIdsExistentes.Contains)
                .ToList(),
            MalIdsEncontrados = malIdsEncontrados,
            MalIdsCorrigidos = malIdsCorrigidos
        });
    }

    private HashSet<int> ObterMalIdsPertencentesAOutraColecao(
        int? myAnimeIdAtual,
        IReadOnlyCollection<int> malIds)
    {
        var idsSolicitados = malIds.ToHashSet();
        var proprietariosDosAnimes = context.Animes
            .AsNoTracking()
            .Where(anime => idsSolicitados.Contains(anime.MalId) && anime.MyAnimeID > 0)
            .ToDictionary(anime => anime.MalId, anime => anime.MyAnimeID);
        var idsIgnorados = proprietariosDosAnimes
            .Where(item => !myAnimeIdAtual.HasValue || item.Value != myAnimeIdAtual.Value)
            .Select(item => item.Key)
            .ToHashSet();

        var colecoes = context.MyAnimes
            .AsNoTracking()
            .Where(colecao => !myAnimeIdAtual.HasValue || colecao.Id != myAnimeIdAtual.Value)
            .ToList();

        foreach (var malId in colecoes
                     .SelectMany(colecao => colecao.AnimesMalId)
                     .Where(idsSolicitados.Contains))
        {
            if (!proprietariosDosAnimes.ContainsKey(malId))
                idsIgnorados.Add(malId);
        }

        return idsIgnorados;
    }

    private int? ObterOutraColecaoQueContemMalId(int malId, int myAnimeIdAtual)
    {
        return context.MyAnimes
            .AsNoTracking()
            .Where(colecao => colecao.Id != myAnimeIdAtual)
            .ToList()
            .FirstOrDefault(colecao => colecao.AnimesMalId.Contains(malId))?
            .Id;
    }
}
