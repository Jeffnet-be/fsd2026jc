using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Services.ViewModels;

namespace ChampionsLeague.Services;

/// <summary>
/// Service-laag contract voor wedstrijd-gerelateerde operaties.
///
/// WAAROM EEN SERVICE-LAAG?
/// De Web-laag (controllers) mag nooit rechtstreeks repositories aanspreken.
/// Repositories kennen alleen hoe data uit de DB gehaald wordt; services kennen
/// de businesslogica (welke data combineren, welke regels gelden).
/// Door controllers enkel met interfaces te laten praten, kan je morgen
/// de SQL-implementatie vervangen door een andere DB zonder één regel in
/// de Web-laag te wijzigen.
/// </summary>
public interface IMatchService
{
    /// <summary>Geeft alle wedstrijden terug met club- en stadion-info.</summary>
    Task<IEnumerable<Match>> GetAllWithClubsAsync();

    /// <summary>Geeft wedstrijden terug gefilterd op club (thuis of uit).</summary>
    Task<IEnumerable<Match>> GetByClubAsync(int clubId);

    /// <summary>
    /// Bouwt het ViewModel voor de wedstrijd-detailpagina.
    /// Combineert match-info, sector-capaciteit en beschikbaarheid in één aanroep.
    /// De controller hoeft geen LINQ of businesslogica te kennen.
    /// </summary>
    Task<ServiceMatchDetailVM?> GetDetailAsync(int matchId);

    /// <summary>Geeft het aantal verkochte (niet-geannuleerde) tickets terug per sector.</summary>
    Task<int> GetSoldCountAsync(int matchId, int sectorId);
}
