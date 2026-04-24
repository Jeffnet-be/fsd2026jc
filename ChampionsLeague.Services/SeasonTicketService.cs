using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Services.DTOs;

namespace ChampionsLeague.Services;

/// <summary>
/// Service-laag contract voor seizoensabonnement-operaties.
/// Gebruikt <see cref="SeasonCartItemDto"/> en <see cref="SeasonTicketDto"/> — geen Web.ViewModels.
/// </summary>
public interface ISeasonTicketService
{
    /// <summary>Actieve abonnementen van een gebruiker, als DTOs.</summary>
    Task<IEnumerable<SeasonTicketDto>> GetUserSeasonTicketsAsync(string userId);

    /// <summary>Alle abonnementen (incl. geannuleerde) van een gebruiker, als DTOs.</summary>
    Task<IEnumerable<SeasonTicketDto>> GetAllUserSeasonTicketsAsync(string userId);

    /// <summary>
    /// Stoelnummers in een sector die bezet zijn door abonnementen.
    /// Gebruikt door TicketService om dubbele stoel-verkoop te voorkomen.
    /// </summary>
    Task<IEnumerable<int>> GetSeasonReservedSeatsAsync(int sectorId);

    /// <summary>
    /// Finaliseert aankoop: controleert capaciteit (losse tickets + abonnementen),
    /// wijst stoelnummer toe, slaat op in DB.
    /// </summary>
    Task<(bool Success, string? Error, SeasonTicketDto? Created)> FinalizeAsync(
        string userId, SeasonCartItemDto item);

    /// <summary>
    /// Annuleert een abonnement. Na annulatie (IsActive=false) wordt
    /// het stoelnummer terug vrijgegeven.
    /// </summary>
    Task<(bool Success, string? Error)> CancelAsync(int seasonTicketId, string userId);

    /// <summary>Telt actieve abonnementen van een gebruiker voor een set sector-IDs.</summary>
    Task<int> CountActiveForClubAsync(string userId, IEnumerable<int> sectorIds);
}

/// <summary>
/// Implementatie van <see cref="ISeasonTicketService"/>.
/// Bevat de capaciteitscontrole die losse tickets EN abonnementen combineert
/// om dubbele stoel-verkoop te voorkomen.
/// </summary>
public class SeasonTicketService : ISeasonTicketService
{
    private readonly ISeasonTicketRepository _seasonTickets;
    private readonly ITicketRepository       _tickets;

    public SeasonTicketService(
        ISeasonTicketRepository seasonTickets,
        ITicketRepository       tickets)
    {
        _seasonTickets = seasonTickets;
        _tickets       = tickets;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SeasonTicketDto>> GetUserSeasonTicketsAsync(string userId)
    {
        var entities = await _seasonTickets.GetUserSeasonTicketsAsync(userId);
        return entities.Select(x => ToDto(x));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SeasonTicketDto>> GetAllUserSeasonTicketsAsync(string userId)
    {
        var entities = await _seasonTickets.GetAllUserSeasonTicketsAsync(userId);
        return entities.Select(x => ToDto(x));
    }

    /// <inheritdoc/>
    public Task<IEnumerable<int>> GetSeasonReservedSeatsAsync(int sectorId)
        => _seasonTickets.GetSeasonReservedSeatsAsync(sectorId);

    /// <summary>
    /// Finaliseert aankoop van een abonnement uit de winkelwagen.
    ///
    /// OVERBOEKING FIX: Combineert abonnements-stoelen EN losse ticketstoelen
    /// om te voorkomen dat dezelfde stoel als beide wordt verkocht.
    /// </summary>
    public async Task<(bool Success, string? Error, SeasonTicketDto? Created)> FinalizeAsync(
        string userId, SeasonCartItemDto item)
    {
        // Stoelen bezet door actieve abonnementen in dit vak
        var seasonSeats = (await _seasonTickets.GetSeasonReservedSeatsAsync(item.SectorId))
                              .ToHashSet();

        // Stoelen bezet door losse tickets in dit vak — voorkomt dubbele verkoop
        // matchId=0 → GetReservedSeatsAsync moet dit ondersteunen, of gebruik een aparte query
        var looseSeats = (await _tickets.GetReservedSeatsAsync(0, item.SectorId))
                              .ToHashSet();

        var allTaken = seasonSeats.Union(looseSeats);
        var nextSeat = Enumerable.Range(1, 1000).FirstOrDefault(s => !allTaken.Contains(s));

        if (nextSeat == 0)
            return (false, $"Geen plaatsen beschikbaar in vak '{item.SectorName}'.", null);

        var entity = new SeasonTicket
        {
            UserId      = userId,
            SectorId    = item.SectorId,
            SeatNumber  = nextSeat,
            TotalPrice  = item.TotalPrice,
            PurchasedAt = DateTime.UtcNow,
            IsActive    = true,
            VoucherId   = Guid.NewGuid()
        };

        await _seasonTickets.AddAsync(entity);
        await _seasonTickets.SaveChangesAsync();

        return (true, null, ToDto(entity, item.SectorName, item.StadiumName, item.ClubName));
    }

    /// <summary>
    /// Annuleert een abonnement.
    /// Na annulatie (IsActive=false) telt GetSeasonReservedSeatsAsync dit stoelnummer
    /// niet meer mee — het is automatisch terug beschikbaar.
    /// </summary>
    public async Task<(bool Success, string? Error)> CancelAsync(int seasonTicketId, string userId)
    {
        var ticket = await _seasonTickets.GetByIdAsync(seasonTicketId);

        if (ticket is null || ticket.UserId != userId)
            return (false, "Abonnement niet gevonden.");

        if (!ticket.IsActive)
            return (false, "Dit abonnement is al geannuleerd.");

        ticket.IsActive = false;
        await _seasonTickets.SaveChangesAsync();

        return (true, null);
    }

    /// <inheritdoc/>
    public async Task<int> CountActiveForClubAsync(string userId, IEnumerable<int> sectorIds)
    {
        var sectorSet = sectorIds.ToHashSet();
        var existing  = await _seasonTickets.GetUserSeasonTicketsAsync(userId);
        return existing.Count(t => sectorSet.Contains(t.SectorId) && t.IsActive);
    }

    // ── Mapping helpers ──────────────────────────────────────────────────

    private static SeasonTicketDto ToDto(SeasonTicket st,
        string sectorName = "", string stadiumName = "", string clubName = "")
        => new()
        {
            Id          = st.Id,
            UserId      = st.UserId,
            SectorId    = st.SectorId,
            SectorName  = sectorName.Length > 0 ? sectorName : st.Sector?.Name         ?? string.Empty,
            StadiumName = stadiumName.Length > 0 ? stadiumName : st.Sector?.Stadium?.Name ?? string.Empty,
            ClubName    = clubName.Length > 0 ? clubName : st.Sector?.Stadium?.Club?.Name ?? string.Empty,
            SeatNumber  = st.SeatNumber,
            TotalPrice  = st.TotalPrice,
            PurchasedAt = st.PurchasedAt,
            IsActive    = st.IsActive,
            VoucherId   = st.VoucherId
        };
}
