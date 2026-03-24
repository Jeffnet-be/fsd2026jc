namespace ChampionsLeague.Web.ViewModels;

/// <summary>ViewModel for the club card on the home page — mapped from Club entity via AutoMapper.</summary>
public class ClubCardVM
{
    public int      Id            { get; set; }
    public string   Name          { get; set; } = string.Empty;
    public string   Country       { get; set; } = string.Empty;
    public string   BadgeUrl      { get; set; } = string.Empty;
    public string   PrimaryColor  { get; set; } = "#000";
    public string   StadiumName   { get; set; } = string.Empty;
    public string   StadiumCity   { get; set; } = string.Empty;
    public int      TotalCapacity { get; set; }
    public List<SectorVM> Sectors { get; set; } = new();
}

/// <summary>ViewModel for one sector row in the stadium capacity table.</summary>
public class SectorVM
{
    public int     Id        { get; set; }
    public string  Name      { get; set; } = string.Empty;
    public int     Capacity  { get; set; }
    public decimal BasePrice { get; set; }
}
