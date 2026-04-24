namespace ChampionsLeague.Web.ViewModels;

/// <summary>
/// ViewModel voor een club-kaart op de homepagina en abonnements-pagina.
///
/// ALLE properties die _ClubCard.cshtml en AutoMapperProfile.cs gebruiken
/// zijn hier aanwezig: PrimaryColor, Country, TotalCapacity, BadgeUrl,
/// StadiumName, StadiumCity, Sectors.
/// </summary>
public class ClubCardVM
{
    public int    Id            { get; set; }
    public string Name          { get; set; } = string.Empty;
    public string Country       { get; set; } = string.Empty;
    public string BadgeUrl      { get; set; } = string.Empty;
    public string PrimaryColor  { get; set; } = "#000000";
    public string StadiumName   { get; set; } = string.Empty;
    public string StadiumCity   { get; set; } = string.Empty;
    public int    TotalCapacity { get; set; }

    /// <summary>
    /// Sectoren voor de tabel in de club-kaart.
    /// Gebruikt SectorVM zodat de bestaande AutoMapper-mapping blijft werken.
    /// </summary>
    public List<SectorVM> Sectors { get; set; } = new();
}

/// <summary>
/// ViewModel voor één sector-rij in de club-kaart tabel.
/// Naam blijft SectorVM zodat AutoMapperProfile.cs ongewijzigd blijft.
/// </summary>
public class SectorVM
{
    public int     Id        { get; set; }
    public string  Name      { get; set; } = string.Empty;
    public int     Capacity  { get; set; }
    public decimal BasePrice { get; set; }
}
