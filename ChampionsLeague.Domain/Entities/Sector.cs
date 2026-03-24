namespace ChampionsLeague.Domain.Entities;

/// <summary>
/// One of the eight predefined vaktypes (sectors) inside a Stadium.
/// Capacity is set per sector; tickets are sold against available seats.
/// </summary>
public class Sector
{
    public int Id { get; set; }

    /// <summary>
    /// Human-readable sector label stored as canonical Dutch name.
    /// Translations live in resource files — the entity stores the key.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Enum-driven type so business rules can target specific sectors.</summary>
    public SectorType Type { get; set; }

    /// <summary>Total number of seats in this sector for every match.</summary>
    public int Capacity { get; set; }

    /// <summary>Base price in EUR for a single ticket in this sector.</summary>
    public decimal BasePrice { get; set; }

    public int StadiumId { get; set; }
    public Stadium Stadium { get; set; } = null!;

    /// <summary>All tickets ever issued for this sector.</summary>
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    /// <summary>Season tickets permanently assigned in this sector.</summary>
    public ICollection<SeasonTicket> SeasonTickets { get; set; } = new List<SeasonTicket>();
}

/// <summary>
/// Canonical enumeration of the eight sector types defined in the project spec.
/// Values match the seed data order 1-8.
/// </summary>
public enum SectorType
{
    LowerBehindHomeGoal  = 1,
    LowerBehindAwayGoal  = 2,
    LowerSideEast        = 3,
    LowerSideWest        = 4,
    UpperBehindHomeGoal  = 5,
    UpperBehindAwayGoal  = 6,
    UpperSideEast        = 7,
    UpperSideWest        = 8
}
