using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Web.ViewModels;

namespace ChampionsLeague.Services;

/// <summary>
/// Service-laag contract voor seizoensabonnement-operaties.
///
/// Bevat zowel query-methoden (voor weergave) als commando-methoden (aankoop, annulatie).
/// De CheckoutController en SeasonTicketController spreken enkel deze interface aan —
/// ze weten niets over EF Core, DbContext of SQL.
/// </summary>
public interface ISeasonTicketService
{
    /// <summary>Geeft actieve abonnementen terug voor een gebruiker.</summary>
    Task<IEnumerable<SeasonTicket>> GetUserSeasonTicketsAsync(string userId);

    /// <summary>Geeft alle abonnementen (inclusief geannuleerde) terug voor een gebruiker.</summary>
    Task<IEnumerable<SeasonTicket>> GetAllUserSeasonTicketsAsync(string userId);

    /// <summary>
    /// Geeft stoelnummers terug die al gereserveerd zijn door seizoensabonnementen
    /// voor een bepaald vak. Gebruikt door TicketService om overboeking te voorkomen.
    /// </summary>
    Task<IEnumerable<int>> GetSeasonReservedSeatsAsync(int sectorId);

    /// <summary>
    /// Finaliseert de aankoop van een abonnement: wijst een stoelnummer toe en
    /// slaat op in de database. Roept validatieregels op (sector vol, seizoen al gestart).
    /// </summary>
    Task<(bool Success, string? Error, SeasonTicket? Created)> FinalizeAsync(
        string userId, SeasonCartItemVM item);

    /// <summary>
    /// Annuleert een seizoensabonnement als de gebruiker de eigenaar is en het nog actief is.
    /// Na annulatie wordt het stoelnummer terug vrijgegeven.
    /// </summary>
    Task<(bool Success, string? Error)> CancelAsync(int seasonTicketId, string userId);

    /// <summary>
    /// Telt hoeveel actieve abonnementen een gebruiker heeft voor sectoren
    /// die tot de opgegeven club behoren. Gebuikt voor de max-4-regel.
    /// </summary>
    Task<int> CountActiveForClubAsync(string userId, IEnumerable<int> sectorIdsForClub);
}
