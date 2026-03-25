namespace ChampionsLeague.Web.Services;

/// <summary>
/// Simple translation service used by views via @inject.
/// Returns the correct string for the currently active culture.
/// This is a lightweight alternative to .resx files — easier to
/// maintain and explain during a defence.
/// </summary>
public class TranslationService
{
    private readonly IHttpContextAccessor _http;

    public TranslationService(IHttpContextAccessor http)
        => _http = http;

    /// <summary>Current two-letter culture code: "nl", "fr", or "en".</summary>
    public string Culture
    {
        get
        {
            var lang = System.Threading.Thread.CurrentThread
                             .CurrentUICulture.TwoLetterISOLanguageName;
            return lang is "nl" or "fr" or "en" ? lang : "nl";
        }
    }

    // ── All translated strings ───────────────────────────────────────
    private static readonly Dictionary<string, Dictionary<string, string>> _t = new()
    {
        ["nav_home"] = new()
        {
            ["nl"] = "Home",
            ["fr"] = "Accueil",
            ["en"] = "Home"
        },
        ["nav_matches"] = new()
        {
            ["nl"] = "Wedstrijden",
            ["fr"] = "Matchs",
            ["en"] = "Matches"
        },
        ["nav_mytickets"] = new()
        {
            ["nl"] = "Mijn tickets",
            ["fr"] = "Mes billets",
            ["en"] = "My Tickets"
        },
        ["nav_login"] = new()
        {
            ["nl"] = "Inloggen",
            ["fr"] = "Connexion",
            ["en"] = "Login"
        },
        ["nav_register"] = new()
        {
            ["nl"] = "Registreren",
            ["fr"] = "S'inscrire",
            ["en"] = "Register"
        },
        ["nav_logout"] = new()
        {
            ["nl"] = "Afmelden",
            ["fr"] = "Déconnexion",
            ["en"] = "Logout"
        },
        ["nav_language"] = new()
        {
            ["nl"] = "Taal",
            ["fr"] = "Langue",
            ["en"] = "Language"
        },
        ["home_hero_title"] = new()
        {
            ["nl"] = "Ticket Portal",
            ["fr"] = "Portail Billetterie",
            ["en"] = "Ticket Portal"
        },
        ["home_hero_subtitle"] = new()
        {
            ["nl"] = "Koop officiële tickets voor de grootste voetbalwedstrijden van Europa.",
            ["fr"] = "Achetez vos billets officiels pour les plus grands matchs d'Europe.",
            ["en"] = "Buy official tickets for the biggest football matches in Europe."
        },
        ["home_view_calendar"] = new()
        {
            ["nl"] = "Bekijk wedstrijdkalender",
            ["fr"] = "Voir le calendrier",
            ["en"] = "View Match Calendar"
        },
        ["home_create_account"] = new()
        {
            ["nl"] = "Account aanmaken",
            ["fr"] = "Créer un compte",
            ["en"] = "Create Account"
        },
        ["home_clubs_title"] = new()
        {
            ["nl"] = "Deelnemende clubs",
            ["fr"] = "Clubs participants",
            ["en"] = "Participating Clubs"
        },
        ["home_feat_tickets"] = new()
        {
            ["nl"] = "Officiële tickets",
            ["fr"] = "Billets officiels",
            ["en"] = "Official Tickets"
        },
        ["home_feat_tickets_sub"] = new()
        {
            ["nl"] = "Unieke voucher per e-mail",
            ["fr"] = "Bon unique par e-mail",
            ["en"] = "Unique voucher by email"
        },
        ["home_feat_fair"] = new()
        {
            ["nl"] = "Eerlijk & veilig",
            ["fr"] = "Juste & sécurisé",
            ["en"] = "Fair & Secure"
        },
        ["home_feat_fair_sub"] = new()
        {
            ["nl"] = "Max 4 tickets per wedstrijd",
            ["fr"] = "Max 4 billets par match",
            ["en"] = "Max 4 tickets per match"
        },
        ["home_feat_hotel"] = new()
        {
            ["nl"] = "Hotel zoeken",
            ["fr"] = "Recherche d'hôtel",
            ["en"] = "Hotel Search"
        },
        ["home_feat_hotel_sub"] = new()
        {
            ["nl"] = "Vlakbij het stadion",
            ["fr"] = "Près du stade",
            ["en"] = "Near the stadium"
        },
        ["stat_clubs"] = new()
        {
            ["nl"] = "Clubs",
            ["fr"] = "Clubs",
            ["en"] = "Clubs"
        },
        ["stat_matches"] = new()
        {
            ["nl"] = "Wedstrijden",
            ["fr"] = "Matchs",
            ["en"] = "Matches"
        },
        ["stat_seats"] = new()
        {
            ["nl"] = "Totaal plaatsen",
            ["fr"] = "Places totales",
            ["en"] = "Total seats"
        },
        ["stat_languages"] = new()
        {
            ["nl"] = "Talen",
            ["fr"] = "Langues",
            ["en"] = "Languages"
        },
        ["match_calendar_title"] = new()
        {
            ["nl"] = "Wedstrijdkalender",
            ["fr"] = "Calendrier des matchs",
            ["en"] = "Match Calendar"
        },
        ["match_calendar_sub"] = new()
        {
            ["nl"] = "Alle Champions League-wedstrijden — klik op Tickets om te kopen",
            ["fr"] = "Tous les matchs de la Ligue des Champions — cliquez sur Billets",
            ["en"] = "All Champions League fixtures — click Tickets to buy"
        },
        ["match_btn_tickets"] = new()
        {
            ["nl"] = "Tickets",
            ["fr"] = "Billets",
            ["en"] = "Tickets"
        },
        ["match_sale_open"] = new()
        {
            ["nl"] = "Open",
            ["fr"] = "Ouvert",
            ["en"] = "Open"
        },
        ["match_sale_closed"] = new()
        {
            ["nl"] = "Gesloten",
            ["fr"] = "Fermé",
            ["en"] = "Closed"
        },
        ["club_view_fixtures"] = new()
        {
            ["nl"] = "Bekijk wedstrijden",
            ["fr"] = "Voir les matchs",
            ["en"] = "View fixtures"
        },
        ["club_seats"] = new()
        {
            ["nl"] = "plaatsen",
            ["fr"] = "places",
            ["en"] = "seats"
        },
        ["footer_tagline"] = new()
        {
            ["nl"] = "Officieel Champions League ticketportaal",
            ["fr"] = "Portail officiel de billetterie Ligue des Champions",
            ["en"] = "Official Champions League ticket portal"
        },
    };

    /// <summary>Returns the translated string for the given key in the current culture.</summary>
    public string T(string key)
    {
        if (_t.TryGetValue(key, out var translations))
            if (translations.TryGetValue(Culture, out var val))
                return val;
        return key; // fallback: return the key itself so nothing is invisible
    }
}
