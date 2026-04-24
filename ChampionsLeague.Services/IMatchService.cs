using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Services.DTOs;

namespace ChampionsLeague.Services;

/// <summary>
/// Service-laag contract voor wedstrijd-gerelateerde operaties.
/// Retourneert <see cref="MatchDto"/> en <see cref="MatchDetailDto"/> (Services.DTOs),
/// NOOIT typen uit ChampionsLeague.Web.ViewModels.
/// </summary>
public interface IMatchService
{
    /// <summary>Alle wedstrijden met club- en stadioninfo.</summary>
    Task<IEnumerable<MatchDto>> GetAllAsync();

    /// <summary>Wedstrijden gefilterd op club (thuis of uit).</summary>
    Task<IEnumerable<MatchDto>> GetByClubAsync(int clubId);

    /// <summary>
    /// Volledige detaildata voor de wedstrijd-detailpagina:
    /// wedstrijd-info + beschikbaarheid per sector.
    /// Geeft null terug als de wedstrijd niet bestaat.
    /// </summary>
    Task<MatchDetailDto?> GetDetailAsync(int matchId);

    /// <summary>
    /// Geeft alle Match-entiteiten terug (met navigatie-eigenschappen).
    /// Gebruikt door CheckoutController en API voor e-mail en mapping.
    /// </summary>
    Task<IEnumerable<Match>> GetAllWithClubsAsync();
}
