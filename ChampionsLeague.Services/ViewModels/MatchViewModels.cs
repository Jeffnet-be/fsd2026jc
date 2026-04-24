using System.ComponentModel.DataAnnotations;

namespace ChampionsLeague.Services.ViewModels;

/// <summary>ViewModel/DTO for a single row in the match calendar list (DataTable).</summary>
public class MatchListItemVM
{
    public int      Id            { get; set; }
    public string   HomeClubName  { get; set; } = string.Empty;
    public string   AwayClubName  { get; set; } = string.Empty;
    public string   HomeClubBadge { get; set; } = string.Empty;
    public string   AwayClubBadge { get; set; } = string.Empty;
    public string   StadiumName   { get; set; } = string.Empty;
    public string   StadiumCity   { get; set; } = string.Empty;
    public DateTime MatchDate     { get; set; }
    public string   Phase         { get; set; } = string.Empty;
    public bool     IsSaleOpen    { get; set; }
}

/// <summary>ViewModel/DTO for the match detail page, including sector availability.</summary>
public class MatchDetailVM
{
    public int      Id            { get; set; }
    public string   HomeClubName  { get; set; } = string.Empty;
    public string   AwayClubName  { get; set; } = string.Empty;
    public string   HomeClubBadge { get; set; } = string.Empty;
    public string   AwayClubBadge { get; set; } = string.Empty;
    public DateTime MatchDate     { get; set; }
    public string   Phase         { get; set; } = string.Empty;
    public string   StadiumName   { get; set; } = string.Empty;
    public string   StadiumCity   { get; set; } = string.Empty;
    public bool     IsSaleOpen    { get; set; }
    public List<SectorAvailabilityVM> Sectors { get; set; } = new();
}

/// <summary>Shows remaining availability in one sector for a match.</summary>
public class SectorAvailabilityVM
{
    public int     SectorId   { get; set; }
    public string  SectorName { get; set; } = string.Empty;
    public int     Capacity   { get; set; }
    public int     Available  { get; set; }
    public decimal Price      { get; set; }
}

/// <summary>
/// Wraps the match list with filter/club data for the calendar index view.
/// Passed to the jQuery DataTable in Views/Matches/Index.cshtml.
/// </summary>
public class MatchListVM
{
    public IEnumerable<MatchListItemVM> Matches    { get; set; } = Enumerable.Empty<MatchListItemVM>();
    public IEnumerable<string>          Clubs      { get; set; } = Enumerable.Empty<string>();
    public string?                      FilterClub { get; set; }
}
