namespace ChampionsLeague.Web.ViewModels;

/// <summary>
/// ViewModel voor een club-kaart op de homepagina en abonnements-pagina.
/// Bevat stadion- en sector-data voor directe weergave.
///
/// NIEUW t.o.v. origineel: SectorCardVM toegevoegd zodat HomeController
/// en SeasonTicketController ClubDtos correct kunnen mappen zonder
/// de Club-entiteit te kennen.
/// </summary>
public class ClubCardVM
{
    public int    Id          { get; set; }
    public string Name        { get; set; } = string.Empty;
    public string BadgeUrl    { get; set; } = string.Empty;
    public string StadiumName { get; set; } = string.Empty;
    public string StadiumCity { get; set; } = string.Empty;
    public List<SectorCardVM> Sectors { get; set; } = new();
}

/// <summary>Sectorgegevens voor weergave in club-kaart en abonnements-keuze.</summary>
public class SectorCardVM
{
    public int     Id        { get; set; }
    public string  Name      { get; set; } = string.Empty;
    public int     Capacity  { get; set; }
    public decimal BasePrice { get; set; }
}
