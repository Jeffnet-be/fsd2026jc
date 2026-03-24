# Champions League Ticket Portal
## Full Stack Development — VIVES Hogeschool

### Prerequisites
- Visual Studio 2022 / 2026 (with ASP.NET workload)
- .NET 9 SDK
- SQL Server 2022 Express or LocalDB
- Node / npm (optional — only if you want to manage client libs manually)

---

### Quick Start (5 steps)

**1. Clone / unzip the project**
```
Unzip ChampionsLeague.zip to a local folder (avoid long paths on Windows).
```

**2. Open the solution**
```
Open ChampionsLeague.sln in Visual Studio.
```

**3. Check the connection string** (`ChampionsLeague.Web/appsettings.json`)
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ChampionsLeagueDb;Trusted_Connection=True;..."
```
Change `(localdb)\\mssqllocaldb` to your SQL Server instance name if needed (e.g. `.\SQL22_VIVES`).

**4. Run migrations (only needed if you prefer CLI over auto-migrate)**

The app calls `db.Database.Migrate()` on startup automatically.
To run manually from the Package Manager Console:
```powershell
# Set ChampionsLeague.Web as the startup project first
Add-Migration InitialCreate -Project ChampionsLeague.Infrastructure -StartupProject ChampionsLeague.Web
Update-Database -Project ChampionsLeague.Infrastructure -StartupProject ChampionsLeague.Web
```

Or with the .NET CLI:
```bash
dotnet ef migrations add InitialCreate \
  --project ChampionsLeague.Infrastructure \
  --startup-project ChampionsLeague.Web

dotnet ef database update \
  --project ChampionsLeague.Infrastructure \
  --startup-project ChampionsLeague.Web
```

**5. Run the project**
```
Press F5 (Debug) or Ctrl+F5 (Run without debug) in Visual Studio.
The browser opens at https://localhost:5001
```

---

### Project Structure

```
ChampionsLeague.sln
├── ChampionsLeague.Domain/          No external dependencies — pure C# entities + interfaces
│   ├── Entities/                    ApplicationUser, Club, Stadium, Sector, Match, Ticket, Order, ...
│   └── Interfaces/                  IRepository<T>, IMatchRepository, ITicketRepository, ...
│
├── ChampionsLeague.Infrastructure/  EF Core implementation
│   ├── Data/                        AppDbContext, SeedData (6 clubs, 48 sectors, 13 matches)
│   ├── Repositories/                BaseRepository<T> + 5 concrete implementations
│   └── Services/                    EmailService (stub), HotelApiService (mock)
│
├── ChampionsLeague.Services/        Business logic — all rules enforced here
│   └── TicketService.cs             PurchaseAsync, CancelAsync, GetAvailableSeatsAsync
│
└── ChampionsLeague.Web/             ASP.NET Core MVC presentation layer
    ├── Controllers/                 Home, Matches, Cart, Checkout, Account, Hotel
    ├── ViewModels/                  DTOs between controllers and views
    ├── AutoMapper/                  AutoMapperProfile — all entity→VM mappings
    ├── Views/                       Razor .cshtml files + _Layout + partials
    ├── Resources/                   .resx files for NL / FR / EN localisation
    └── wwwroot/                     CSS, JS, static assets
```

---

### Curriculum Alignment

| Curriculum Concept               | Location |
|----------------------------------|----------|
| EF Core / DbContext / DbSet      | `Infrastructure/Data/AppDbContext.cs` |
| Code-First migrations + Fluent API | `AppDbContext.OnModelCreating()` |
| Repository pattern               | `Domain/Interfaces/` + `Infrastructure/Repositories/` |
| Dependency Injection             | `Web/Program.cs` — all `builder.Services.Add*` calls |
| AutoMapper                       | `Web/AutoMapper/AutoMapperProfile.cs` |
| LINQ queries                     | All `*Repository.cs` files |
| Data Annotations + Validation    | `Web/ViewModels/AccountViewModels.cs`, `HotelViewModels.cs` |
| Partial Views                    | `Views/Shared/_ClubCard.cshtml`, `_CartSummary.cshtml` |
| jQuery DataTables                | `Views/Matches/Index.cshtml`, `Views/Account/MyTickets.cshtml` |
| AJAX / Unobtrusive               | `Views/Matches/Detail.cshtml` — AddToCart AJAX call |
| ASP.NET Core Identity            | `ApplicationUser`, `Program.cs` Identity config, `AccountController` |
| Localisation (NL/FR/EN)         | `Resources/*.resx`, `Program.cs` localisation middleware |
| HttpClient (external API)        | `Infrastructure/Services/HotelApiService.cs` |
| Session (shopping cart)          | `CartController.cs` + `Program.cs` AddSession |
| Multilayer architecture          | 4-project solution with strict dependency direction |
| Service layer                    | `Services/TicketService.cs` |
| ViewModel / Model separation     | All `ViewModels/*.cs` files |

---

### Business Rules Implemented

| Rule | Enforcement location |
|------|---------------------|
| Tickets on sale 1 month before match | `Match.IsSaleOpen` (computed) + `TicketService.PurchaseAsync` |
| Max 4 tickets per person per match | `CartController.AddToCart` (UX) + `TicketService.PurchaseAsync` (authoritative) |
| No two matches on same day | `OrderRepository.UserHasMatchOnDayAsync` + `TicketService.PurchaseAsync` |
| Season seats blocked for single tickets | `SeasonTicketRepository.GetSeasonReservedSeatsAsync` in `TicketService` |
| No overbooking | Seat enumeration in `TicketService` + UNIQUE index on VoucherId |
| Free cancellation ≤ 7 days before match | `Match.IsCancellable` (computed) + `TicketService.CancelAsync` |
| Season tickets only before competition | Enforced by business rule in `SeasonTicketRepository` (buy-flow not shown in UI, reserved for extension) |

---

### Test Users (created at first run)

No seed users are created automatically. Register via `/Account/Register`.
The first registered user gets the "User" role automatically.

---

### Extending the Project

- **Real email**: Replace `EmailService.SendAsync` stub with MailKit SMTP call
- **Real payment**: Add Stripe/Mollie checkout before `OrderStatus.Paid`
- **PDF vouchers**: Add QuestPDF to generate printable A4 voucher with QR code
- **Admin panel**: Add `[Authorize(Roles="Admin")]` controllers for club/match management
- **SignalR**: Push live seat-availability updates to the match detail page
- **Azure**: Deploy DB to Azure SQL, web app to Azure App Service via GitHub Actions
