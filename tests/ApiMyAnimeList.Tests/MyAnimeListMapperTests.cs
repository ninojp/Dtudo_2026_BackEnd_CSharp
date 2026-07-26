using ApiMyAnimeList.Dtos;
using ApiMyAnimeList.Mappers;

namespace ApiMyAnimeList.Tests;

public class MyAnimeListMapperTests
{
    [Fact]
    public void MapSearch_MapsPagedAnimeNodesToSharedContract()
    {
        var response = new MalPagedResponse<MalAnimeNode>
        {
            Data =
            [
                new MalListItem<MalAnimeNode>
                {
                    Node = new MalAnimeNode
                    {
                        Id = 21,
                        Title = "One Piece",
                        MainPicture = new MalPicture { Medium = "medium.jpg", Large = "large.jpg" },
                        AlternativeTitles = new MalAlternativeTitles
                        {
                            English = "One Piece",
                            Japanese = "ワンピース",
                            Synonyms = ["OP"]
                        },
                        MediaType = "tv",
                        NumEpisodes = 1000,
                        Mean = 8.7,
                        StartSeason = new MalSeason { Year = 1999 },
                        Genres = [new MalNamedItem { Name = "Action" }]
                    }
                }
            ],
            Paging = new MalPaging { Next = "next-page" }
        };

        var result = MyAnimeListMapper.MapSearch(response, page: 1, limit: 20);

        Assert.True(result.HasNextPage);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Results);
        Assert.Equal(21, result.Results[0].MalId);
        Assert.Equal("large.jpg", result.Results[0].ImageUrl);
        Assert.Contains("Action", result.Results[0].Genres);
    }

    [Fact]
    public void MapRelations_GroupsRelationsByType()
    {
        var anime = new MalAnimeNode
        {
            RelatedAnime =
            [
                new MalRelatedAnime
                {
                    RelationType = "sequel",
                    Node = new MalAnimeNode
                    {
                        Id = 22,
                        Title = "One Piece Sequel",
                        MainPicture = new MalPicture { Medium = "m.jpg", Large = "l.jpg" }
                    }
                }
            ]
        };

        var result = MyAnimeListMapper.MapRelations(anime);

        Assert.Single(result);
        Assert.Equal("sequel", result[0].Relation);
        Assert.Equal(22, result[0].Entry[0].MalId);
        Assert.Equal("l.jpg", result[0].Entry[0].ImageUrl);
    }
}
