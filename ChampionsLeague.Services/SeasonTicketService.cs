using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Services.DTOs;

namespace ChampionsLeague.Services;

/// <summary>
/// Service-laag contract voor seizoensabonnement-operaties.
/// </summary>
public interface ISeasonTicketService
{
    Task<IEnumerable<SeasonTicketDto>> GetUserSeasonTicketsAsync(string userId);
    Task<IEnumerable<SeasonTicketDto>> GetAllUserSeasonTicketsAsync(string userId);
    Task<IEnumerable<int>> GetSeasonReservedSeatsAsync(int sectorId);
    Task<(bool Success, string? Error, SeasonTicketDto? Created)> FinalizeAsync(
        string userId, SeasonCartItemDto item);
    Task<(bool Success, string? Error)> CancelAsync(int seasonTicketId, string userId);
    Task<int> CountActiveForClubAsync(string userId, IEnumerable<int> sectorIds);
}

/// <summary>
/// Stoeltoewijzing combineert:
///   - Actieve abonnementen in het vak (IsActive == true)
///   - Losse tickets in het vak over ALLE wedstrijden (Status != Cancelled)
/// Capaciteitsgrens = echte sector.Capacity uit de database, niet hardcoded 1000.
/// </summary>
public class SeasonTicketService : ISeasonTicketService
{
    private readonly ISeasonTicketRepository _seasonTickets;
    private readonly ITicketRepository       _tickets;
    private readonly IClubRepository         _clubs;

    public SeasonTicketService(
        ISeasonTicketRepository seasonTickets,
        ITicketRepository       tickets,
        IClubRepository         clubs)
    {
        _seasonTickets = seasonTickets;
        _tickets       = tickets;
        _clubs         = clubs;
    }

    public async Task<IEnumerable<SeasonTicketDto>> GetUserSeasonTicketsAsync(string userId)
    {
        var entities = await _seasonTickets.GetUserSeasonTicketsAsync(userId);
        return entities.Select(x => ToDto(x));
    }

    public async Task<IEnumerable<SeasonTicketDto>> GetAllUserSeasonTicketsAsync(string userId)
    {
        var entities = await _seasonTickets.GetAllUserSeasonTicketsAsync(userId);
        return entities.Select(x => ToDto(x));
    }

    public Task<IEnumerable<int>> GetSeasonReservedSeatsAsync(int sectorId)
        => _seasonTickets.GetSeasonReservedSeatsAsync(sectorId);

    /// <summary>
    /// Finaliseert aankoop van een abonnement.
    /// Stoelenlogica:
    /// 1. Bezette abonnementsstoelen (IsActive == true).
    /// 2. Bezette losse-ticket-stoelen over ALLE wedstrijden (Status != Cancelled).
    /// 3. Eerste vrije stoel binnen de echte sector.Capacity.
    /// </summary>
    public async Task<(bool Success, string? Error, SeasonTicketDto? Created)> FinalizeAsync(
        string userId, SeasonCartItemDto item)
    {
        var seasonSeats = (await _seasonTickets.GetSeasonReservedSeatsAsync(item.SectorId))
                              .ToHashSet();

        var looseSeats = (await _tickets.GetAllReservedSeatsInSectorAsync(item.SectorId))
                              .ToHashSet();

        var allTaken = seasonSeats.Union(looseSeats).ToHashSet();

        // Echte capaciteit ophalen — niet hardcoded 1000
        var sector   = await _clubs.GetSectorByIdAsync(item.SectorId);
        int capacity = sector?.Capacity ?? 1000;

        var nextSeat = Enumerable.Range(1, capacity)
                                 .FirstOrDefault(s => !allTaken.Contains(s));

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

    public async Task<int> CountActiveForClubAsync(string userId, IEnumerable<int> sectorIds)
    {
        var sectorSet = sectorIds.ToHashSet();
        var existing  = await _seasonTickets.GetUserSeasonTicketsAsync(userId);
        return existing.Count(t => sectorSet.Contains(t.SectorId) && t.IsActive);
    }

    private static SeasonTicketDto ToDto(SeasonTicket st,
        string sectorName = "", string stadiumName = "", string clubName = "")
        => new()
        {
            Id          = st.Id,
            UserId      = st.UserId,
            SectorId    = st.SectorId,
            SectorName  = sectorName.Length  > 0 ? sectorName  : st.Sector?.Name                ?? string.Empty,
            StadiumName = stadiumName.Length > 0 ? stadiumName : st.Sector?.Stadium?.Name       ?? string.Empty,
            ClubName    = clubName.Length    > 0 ? clubName    : st.Sector?.Stadium?.Club?.Name ?? string.Empty,
            SeatNumber  = st.SeatNumber,
            TotalPrice  = st.TotalPrice,
            PurchasedAt = st.PurchasedAt,
            IsActive    = st.IsActive,
            VoucherId   = st.VoucherId
        };
}
