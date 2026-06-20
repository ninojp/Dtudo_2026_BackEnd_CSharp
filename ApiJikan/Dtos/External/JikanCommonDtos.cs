namespace ApiJikan.Dtos.External;

/// <summary>
/// DTO comum para paginação retornada pela Jikan.
/// </summary>
public class JikanPaginationDto
{
    public int Last_Visible_Page { get; set; }
    public bool Has_Next_Page { get; set; }
    public int Current_Page { get; set; }
    public JikanPaginationItemsDto? Items { get; set; }
}

/// <summary>
/// DTO comum para informações resumidas da paginação da Jikan.
/// </summary>
public class JikanPaginationItemsDto
{
    public int Count { get; set; }
    public int Total { get; set; }
    public int Per_Page { get; set; }
}

/// <summary>
/// DTO de variante de imagem retornada pela Jikan.
/// </summary>
public class JikanImageVariantDto
{
    public string? Image_Url { get; set; }
    public string? Small_Image_Url { get; set; }
    public string? Large_Image_Url { get; set; }
}

/// <summary>
/// DTO comum para trailer retornado pela Jikan.
/// </summary>
public class JikanTrailerDto
{
    //public string? Youtube_Id { get; set; }
    //public string? Url { get; set; }
    public string? Embed_Url { get; set; }
    //public Dictionary<string, JikanImageVariantDto>? Images { get; set; }
}

/// <summary>
/// DTO comum para períodos de exibição retornados pela Jikan.
/// </summary>
public class JikanAiredDto
{
    //public string? From { get; set; }
    //public string? To { get; set; }
    //public JikanPropDto? Prop { get; set; }
    public string? String { get; set; }
}

/// <summary>
/// DTO comum para subpropriedades de datas da Jikan.
/// </summary>
//public class JikanPropDto
//{
    //public JikanDateInfoDto? From { get; set; }
    //public JikanDateInfoDto? To { get; set; }
//}

/// <summary>
/// DTO comum para partes de data retornadas pela Jikan.
/// </summary>
//public class JikanDateInfoDto
//{
//    public int? Day { get; set; }
//    public int? Month { get; set; }
//    public int? Year { get; set; }
//}

/// <summary>
/// DTO comum para itens nomeados retornados pela Jikan.
/// </summary>
public class JikanNamedItemDto
{
    //public int Mal_Id { get; set; }
    //public string? Type { get; set; }
    public string? Name { get; set; }
    //public string? Url { get; set; }
}

/// <summary>
/// DTO comum para entrada de relação retornada pela Jikan.
/// </summary>
public class JikanRelationEntryDto
{
    public int Mal_Id { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }
}
