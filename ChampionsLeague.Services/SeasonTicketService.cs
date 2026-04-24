using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Services.ViewModels;

namespace ChampionsLeague.Services;

/// <summary>
/// Implementatie van <see cref="ISeasonTicketService"/>.
/// Centraliseert alle businesslogica rond seizoensabonnementen:
/// - Capaciteitscontrole (sector vol? dubbelverkoop met los ticket?)
/// - Max-4-abonnementen-per-club per gebruiker
/// - Annulatie (inclusief herbeschikbaarstelling van het stoelnummer)
///
/// De AccountController en SeasonTicketController spreken enkel ISeasonTicketService aan.
/// Ze bevatten geen LINQ, geen DbContext en geen directe repository-calls meer.
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
    public Task<IEnumerable<SeasonTicket>> GetUserSeasonTicketsAsync(string userId)
        => _seasonTickets.GetUserSeasonTicketsAsync(userId);

    /// <inheritdoc/>
    public Task<IEnumerable<SeasonTicket>> GetAllUserSeasonTicketsAsync(string userId)
        => _seasonTickets.GetAllUserSeasonTicketsAsync(userId);

    /// <inheritdoc/>
    public Task<IEnumerable<int>> GetSeasonReservedSeatsAsync(int sectorId)
        => _seasonTickets.GetSeasonReservedSeatsAsync(sectorId);

    /// <summary>
    /// Finaliseert een abonnements-aankoop uit de winkelwagen.
    ///
    /// Controles (in volgorde):
    /// 1. Zijn er vrije stoelen in het vak (rekening houdend met losse tickets EN abonnementen)?
    ///    → Overboeking-preventie: stoelen bezet door losse tickets tellen mee.
    /// 2. Wijs het eerst beschikbare stoelnummer toe.
    /// 3. Sla op in de database.
    ///
    /// Waarom controle op losse tickets? Business rule: "Een abonnements-stoel kan niet meer
    /// als los ticket verkocht worden" — maar het omgekeerde geldt ook: als er al losse tickets
    /// zijn voor een stoel, mag die niet als abonnement verkocht worden.
    /// </summary>
    public async Task<(bool Success, string? Error, SeasonTicket? Created)> FinalizeAsync(
        string userId, ServiceSeasonCartItemVM item)
    {
        // Stoelen bezet door actieve abonnementen in dit vak
        var seasonSeats = (await _seasonTickets.GetSeasonReservedSeatsAsync(item.SectorId))
                              .ToHashSet();

        // Stoelen bezet door losse tickets in dit vak (over alle wedstrijden)
        // → voorkomt dubbelverkoop stoel als abonnement + los ticket
        var looseSeats = (await _tickets.GetReservedSeatsAsync(0, item.SectorId))
                              .ToHashSet();

        var allTaken = seasonSeats.Union(looseSeats).ToHashSet();

        // Zoek het eerste vrije stoelnummer (1-gebaseerd, max 1000)
        var nextSeat = Enumerable.Range(1, 1000).FirstOrDefault(s => !allTaken.Contains(s));

        if (nextSeat == 0)
            return (false, $"No seats available in sector '{item.SectorName}'.", null);

        var seasonTicket = new SeasonTicket
        {
            UserId      = userId,
            SectorId    = item.SectorId,
            SeatNumber  = nextSeat,
            TotalPrice  = item.TotalPrice,
            PurchasedAt = DateTime.UtcNow,
            IsActive    = true,
            VoucherId   = Guid.NewGuid()
        };

        await _seasonTickets.AddAsync(seasonTicket);
        await _seasonTickets.SaveChangesAsync();

        return (true, null, seasonTicket);
    }

    /// <summary>
    /// Annuleert een seizoensabonnement.
    ///
    /// Na annulatie (IsActive = false) telt het stoelnummer niet meer mee in
    /// GetSeasonReservedSeatsAsync (dat filtert op IsActive == true), waardoor
    /// het stoelnummer automatisch terug beschikbaar wordt voor nieuwe aankopen.
    ///
    /// Bug fix: de AccountController deed dit vroeger rechtstreeks via de repository
    /// zonder businesslogica-controles. Die controles zitten nu hier.
    /// </summary>
    public async Task<(bool Success, string? Error)> CancelAsync(int seasonTicketId, string userId)
    {
        var ticket = await _seasonTickets.GetByIdAsync(seasonTicketId);

        if (ticket is null || ticket.UserId != userId)
            return (false, "Season ticket not found.");

        if (!ticket.IsActive)
            return (false, "This season ticket is already cancelled.");

        ticket.IsActive = false;
        await _seasonTickets.SaveChangesAsync();

        return (true, null);
    }

    /// <inheritdoc/>
    public async Task<int> CountActiveForClubAsync(string userId, IEnumerable<int> sectorIdsForClub)
    {
        var sectorSet        = sectorIdsForClub.ToHashSet();
        var existingInDb     = await _seasonTickets.GetUserSeasonTicketsAsync(userId);
        return existingInDb.Count(t => sectorSet.Contains(t.SectorId) && t.IsActive);
    }
}
