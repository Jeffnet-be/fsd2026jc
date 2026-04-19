# Champions League Ticket Portal

> Full Stack Development — VIVES Hogeschool | Toegepaste Informatica | 2025–2026  
> Student: **Jeffrey Clauwaert**

---

## Live applicatie

| Omgeving | URL |
|----------|-----|
| Website | https://fsd2026jc-gmb0dkhag5h7h2ck.westeurope-01.azurewebsites.net |
| Swagger API | https://fsd2026jc-gmb0dkhag5h7h2ck.westeurope-01.azurewebsites.net/swagger |
---

## Projectbeschrijving

Een centrale portaalsite voor de online verkoop van voetbaltickets en seizoensabonnementen voor Champions League-wedstrijden. Gebruikers kunnen tickets kopen voor wedstrijden van 6 deelnemende clubs, een seizoensabonnement aanschaffen, hotels zoeken nabij het stadion en hun aankoophistoriek raadplegen.

### Deelnemende clubs
- Real Madrid
- Manchester City
- FC Bayern München
- Paris Saint-Germain
- Club Brugge
- FC Barcelona

---

## Architectuur

De oplossing is opgesplitst in 4 Visual Studio-projecten volgens het lagenmodel:
ChampionsLeague.sln
├── ChampionsLeague.Domain/         ← Entiteiten & interfaces (geen externe dependencies)
├── ChampionsLeague.Infrastructure/ ← EF Core, repositories, e-mail, hotel API
├── ChampionsLeague.Services/       ← Businesslogica (TicketService)
└── ChampionsLeague.Web/            ← ASP.NET Core MVC, controllers, views, AutoMapper

---

## Technologieën

| Technologie | Gebruik |
|-------------|---------|
| ASP.NET Core MVC (.NET 9) | Webframework |
| Entity Framework Core 9 | ORM — Code-First met migraties |
| Azure SQL Server | Productiedatabase |
| ASP.NET Core Identity | Authenticatie & autorisatie |
| AutoMapper 12 | Entity → ViewModel mapping |
| MailKit | E-mail voor vouchers en wachtwoord reset |
| Swashbuckle / Swagger | API documentatie |
| Bootstrap 5 + DataTables | Frontend |
| GitHub Actions | CI/CD naar Azure App Service |

---

## Database verbinden via SSMS

| Instelling | Waarde |
|-----------|--------|
| Server name | fsd2026jc.database.windows.net |
| Authentication | SQL Server Authentication |
| Login | *Te verkrijgen op aanvraag* |
| Database | *Te verkrijgen op aanvraag* |
| Encrypt | Mandatory |
| Trust server certificate | True |

> Uw IP-adres moet toegevoegd worden aan de Azure firewall:  
> Tot en met 25/04/2026 staat de database open voor alle inkomende verbindingen.
> Na deze datum krijgt u enkel toegang na aanvraag.

---

## Lokaal draaien

```bash
# 1. Clone het project
git clone https://github.com/Jeffnet-be/fsd2026jc.git

# 2. Open ChampionsLeague.sln in Visual Studio 2022+

# 3. Controleer de connection string in:
#    ChampionsLeague.Web/appsettings.json

# 4. Druk op F5 — migraties worden automatisch uitgevoerd bij opstarten
```

---

## Business rules

- Tickets kunnen gekocht worden vanaf **1 maand vóór de wedstrijd**
- Maximum **4 tickets** per persoon per wedstrijd
- Geen tickets voor **twee wedstrijden op dezelfde dag**
- Abonnementen enkel beschikbaar **vóór de competitiestart** (25 april 2026)
- Een abonnementsplaats kan **niet als los ticket** verkocht worden
- **Gratis annulatie** mogelijk tot 1 week vóór de wedstrijd
- Per ticket wordt een **voucher** gegenereerd en per e-mail verstuurd

---

## Projectstructuur
ChampionsLeague.Domain/
├── Entities/          ← Club, Stadium, Sector, Match, Order, Ticket, SeasonTicket
└── Interfaces/        ← IClubRepository, IMatchRepository, IOrderRepository, ...
ChampionsLeague.Infrastructure/
├── Data/              ← AppDbContext, SeedData
├── Repositories/      ← BaseRepository<T>, ClubRepository, MatchRepository, ...
└── Services/          ← EmailService, HotelApiService
ChampionsLeague.Services/
└── TicketService.cs   ← PurchaseAsync, CancelAsync, businessregels
ChampionsLeague.Web/
├── Controllers/       ← Home, Account, Cart, Checkout, SeasonTicket, Hotel
├── ViewModels/        ← CartVM, MatchViewModels, TicketHistoryItemVM, ...
├── Views/             ← Razor views per controller
├── AutoMapper/        ← AutoMapperProfile.cs
├── Services/          ← TranslationService (NL/FR/EN)
└── Resources/         ← Lokalisatiebestanden

---

## CI/CD

Elke push naar de `main`-branch triggert automatisch een GitHub Actions workflow die het project bouwt en deployt naar Azure App Service.

Zie `.github/workflows/azure-deploy.yml` voor de configuratie.
