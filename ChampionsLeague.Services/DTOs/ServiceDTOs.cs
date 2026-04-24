namespace ChampionsLeague.Services.DTOs;

// ── Match DTOs ────────────────────────────────────────────────────────────

public class MatchDto
{
    public int      Id            { get; set; }
    public int      HomeClubId    { get; set; }
    public string   HomeClubName  { get; set; } = string.Empty;
    public string   HomeClubBadge { get; set; } = string.Empty;
    public int      AwayClubId    { get; set; }
    public string   AwayClubName  { get; set; } = string.Empty;
    public string   AwayClubBadge { get; set; } = string.Empty;
    public DateTime MatchDate     { get; set; }
    public string   Phase         { get; set; } = string.Empty;
    public string   StadiumName   { get; set; } = string.Empty;
    public string   StadiumCity   { get; set; } = string.Empty;
    public bool     IsSaleOpen    { get; set; }
}

public class SectorAvailabilityDto
{
    public int     SectorId   { get; set; }
    public string  SectorName { get; set; } = string.Empty;
    public int     Capacity   { get; set; }
    public int     Available  { get; set; }
    public decimal Price      { get; set; }
}

public class MatchDetailDto
{
    public MatchDto                    Match   { get; set; } = new();
    public List<SectorAvailabilityDto> Sectors { get; set; } = new();
}

// ── Club DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// Club-data die de services-laag teruggeeft.
/// Bevat alle velden die de Web-laag nodig heeft om ClubCardVM te vullen,
/// inclusief PrimaryColor, Country en TotalCapacity.
/// </summary>
public class ClubDto
{
    public int    Id            { get; set; }
    public string Name          { get; set; } = string.Empty;
    public string Country       { get; set; } = string.Empty;
    public string BadgeUrl      { get; set; } = string.Empty;
    public string PrimaryColor  { get; set; } = "#000000";
    public string StadiumName   { get; set; } = string.Empty;
    public string StadiumCity   { get; set; } = string.Empty;
    public int    TotalCapacity { get; set; }
    public List<SectorDto> Sectors { get; set; } = new();
}

public class SectorDto
{
    public int     Id        { get; set; }
    public string  Name      { get; set; } = string.Empty;
    public int     Capacity  { get; set; }
    public decimal BasePrice { get; set; }
}

// ── Season ticket DTOs ────────────────────────────────────────────────────

public class SeasonCartItemDto
{
    public int     SectorId    { get; set; }
    public string  SectorName  { get; set; } = string.Empty;
    public string  StadiumName { get; set; } = string.Empty;
    public string  ClubName    { get; set; } = string.Empty;
    public decimal TotalPrice  { get; set; }
}

public class SeasonTicketDto
{
    public int      Id          { get; set; }
    public string   UserId      { get; set; } = string.Empty;
    public int      SectorId    { get; set; }
    public string   SectorName  { get; set; } = string.Empty;
    public string   StadiumName { get; set; } = string.Empty;
    public string   ClubName    { get; set; } = string.Empty;
    public int      SeatNumber  { get; set; }
    public decimal  TotalPrice  { get; set; }
    public DateTime PurchasedAt { get; set; }
    public bool     IsActive    { get; set; }
    public Guid     VoucherId   { get; set; }
}

// ── Ticket DTOs ───────────────────────────────────────────────────────────

public class TicketHistoryDto
{
    public int      Id               { get; set; }
    public string   MatchDescription { get; set; } = string.Empty;
    public DateTime MatchDate        { get; set; }
    public string   SectorName       { get; set; } = string.Empty;
    public int      SeatNumber       { get; set; }
    public decimal  PricePaid        { get; set; }
    public Guid     VoucherId        { get; set; }
    public string   Status           { get; set; } = string.Empty;
    public bool     IsCancellable    { get; set; }
}

// ── Account DTOs ──────────────────────────────────────────────────────────

public class UserDto
{
    public string Id        { get; set; } = string.Empty;
    public string Email     { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
}

public class RegisterResultDto
{
    public bool         Success       { get; set; }
    public string?      CreatedUserId { get; set; }
    public List<string> Errors        { get; set; } = new();
}

public class RegisterDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public string Email     { get; set; } = string.Empty;
    public string Password  { get; set; } = string.Empty;
}
