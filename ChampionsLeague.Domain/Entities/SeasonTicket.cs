namespace ChampionsLeague.Domain.Entities;

/// <summary>
/// An abonnement (season ticket) giving the holder a fixed seat for all home matches
/// of one club during the season.
/// Business rules:
///   - Must be purchased before competition start.
///   - The associated seat cannot be sold as a single-game ticket (enforced in TicketService).
/// </summary>
public class SeasonTicket
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int SectorId { get; set; }
    public Sector Sector { get; set; } = null!;

    /// <summary>Seat permanently assigned for the full season.</summary>
    public int SeatNumber { get; set; }

    /// <summary>Total price paid for the entire season.</summary>
    public decimal TotalPrice { get; set; }

    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set to false when the season ends or the subscription is cancelled.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Unique voucher code e-mailed to the buyer as proof of purchase.</summary>
    public Guid VoucherId { get; set; } = Guid.NewGuid();
}
