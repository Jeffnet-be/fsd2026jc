using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Web.ViewModels;

namespace ChampionsLeague.Services;

/// <summary>
/// Service-laag contract voor club- en stadion-gerelateerde operaties.
///
/// Scheidt de business-query-logica (welke data heb ik nodig, hoe combineer ik die)
/// van de Web-laag (hoe toon ik die). Controllers injecteren enkel deze interface —
/// ze weten niet of de data uit SQL, een cache of een externe API komt.
/// </summary>
public interface IClubService
{
    /// <summary>Geeft alle clubs terug met hun stadion en sectoren.</summary>
    Task<IEnumerable<Club>> GetAllWithStadiumsAsync();

    /// <summary>Geeft alle clubs terug (zonder stadion-details).</summary>
    Task<IEnumerable<Club>> GetAllAsync();

    /// <summary>
    /// Geeft één club terug met stadion en sectoren op basis van clubId.
    /// Gebruikt door SeasonTicketService om sectorinfo op te halen.
    /// </summary>
    Task<Club?> GetWithStadiumAndSectorsAsync(int clubId);
}
