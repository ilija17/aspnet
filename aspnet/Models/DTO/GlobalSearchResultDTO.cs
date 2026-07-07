namespace aspnet.Models.DTO;

public class GlobalSearchResultDTO
{
    public List<SearchHitDTO> Hits { get; set; } = new();
}

public class SearchHitDTO
{
    public string Type { get; set; }
    public int Id { get; set; }
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public string Url { get; set; }
}
