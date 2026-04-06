namespace ChampionsLeague.Web.ViewModels;

/// <summary>
/// Session-stored shopping cart, serialised as JSON in the session store.
/// Total and Count are computed from the items list — never stored.
/// </summary>
public class CartVM
{
    public List<CartItemVM>       Items       { get; set; } = new();
    public List<SeasonCartItemVM> SeasonItems { get; set; } = new();
    public decimal Total => Items.Sum(i => i.Subtotal) + SeasonItems.Sum(i => i.TotalPrice);
    public int     Count => Items.Sum(i => i.Quantity) + SeasonItems.Count;
}

/// <summary>One season ticket item in the cart.</summary>
public class SeasonCartItemVM
{
    public int     SectorId    { get; set; }
    public string  SectorName  { get; set; } = string.Empty;
    public string  StadiumName { get; set; } = string.Empty;
    public string  ClubName    { get; set; } = string.Empty;
    public decimal TotalPrice  { get; set; }
}

/// <summary>One line in the cart — one match + sector combination.</summary>
public class CartItemVM
{
    public int      MatchId          { get; set; }
    public string   MatchDescription { get; set; } = string.Empty;
    public DateTime MatchDate        { get; set; }
    public int      SectorId         { get; set; }
    public string   SectorName       { get; set; } = string.Empty;
    public int      Quantity         { get; set; }
    public decimal  UnitPrice        { get; set; }
    public decimal  Subtotal         => Quantity * UnitPrice;
}
