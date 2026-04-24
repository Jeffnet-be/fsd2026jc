namespace ChampionsLeague.Web.Services;

/// <summary>
/// Central translation service injected into every view via _ViewImports.cshtml.
/// Reads Thread.CurrentUICulture (set by CookieRequestCultureProvider) and returns
/// the matching string. Fallback is always Dutch so nothing is ever invisible.
/// Two dictionaries: _t for UI strings, _sectors for DB-stored sector names.
/// </summary>
public class TranslationService
{
    public string Culture
    {
        get
        {
            var lang = System.Threading.Thread.CurrentThread
                             .CurrentUICulture.TwoLetterISOLanguageName;
            return lang is "nl" or "fr" or "en" ? lang : "nl";
        }
    }

    private static readonly Dictionary<string, Dictionary<string, string>> _t = new()
    {
        // ── Navbar ────────────────────────────────────────────────────
        ["nav_home"]      = new(){ ["nl"]="Home",          ["fr"]="Accueil",       ["en"]="Home" },
        ["nav_matches"]   = new(){ ["nl"]="Wedstrijden",   ["fr"]="Matchs",        ["en"]="Matches" },
        ["nav_mytickets"] = new(){ ["nl"]="Mijn tickets",  ["fr"]="Mes billets",   ["en"]="My Tickets" },
        ["nav_login"]     = new(){ ["nl"]="Inloggen",      ["fr"]="Connexion",     ["en"]="Login" },
        ["nav_register"]  = new(){ ["nl"]="Registreren",   ["fr"]="S'inscrire",    ["en"]="Register" },
        ["nav_logout"]    = new(){ ["nl"]="Afmelden",      ["fr"]="Déconnexion",   ["en"]="Logout" },
        ["nav_language"]  = new(){ ["nl"]="Taal",          ["fr"]="Langue",        ["en"]="Language" },
        ["nav_cart"]      = new(){ ["nl"]="Winkelwagen",   ["fr"]="Panier",        ["en"]="Cart" },
        ["nav_season"]    = new(){ ["nl"]="Abonnementen",  ["fr"]="Abonnements",   ["en"]="Season Tickets" },

        // ── Footer ────────────────────────────────────────────────────
        ["footer_tagline"] = new(){
            ["nl"]="Officieel Champions League ticketportaal",
            ["fr"]="Portail officiel de billetterie Ligue des Champions",
            ["en"]="Official Champions League ticket portal" },

        // ── Home page ─────────────────────────────────────────────────
        ["home_hero_title"]       = new(){ ["nl"]="Ticket Portal",    ["fr"]="Portail Billetterie",   ["en"]="Ticket Portal" },
        ["home_hero_subtitle"]    = new(){
            ["nl"]="Koop officiële tickets voor de grootste voetbalwedstrijden van Europa.",
            ["fr"]="Achetez vos billets officiels pour les plus grands matchs d'Europe.",
            ["en"]="Buy official tickets for the biggest football matches in Europe." },
        ["home_view_calendar"]    = new(){ ["nl"]="Bekijk wedstrijdkalender", ["fr"]="Voir le calendrier",    ["en"]="View Match Calendar" },
        ["home_create_account"]   = new(){ ["nl"]="Account aanmaken",         ["fr"]="Créer un compte",       ["en"]="Create Account" },
        ["home_clubs_title"]      = new(){ ["nl"]="Deelnemende clubs",         ["fr"]="Clubs participants",    ["en"]="Participating Clubs" },
        ["home_feat_tickets"]     = new(){ ["nl"]="Officiële tickets",         ["fr"]="Billets officiels",     ["en"]="Official Tickets" },
        ["home_feat_tickets_sub"] = new(){ ["nl"]="Unieke voucher per e-mail", ["fr"]="Bon unique par e-mail", ["en"]="Unique voucher by email" },
        ["home_feat_fair"]        = new(){ ["nl"]="Eerlijk & veilig",          ["fr"]="Juste & sécurisé",      ["en"]="Fair & Secure" },
        ["home_feat_fair_sub"]    = new(){ ["nl"]="Max 4 tickets per wedstrijd",["fr"]="Max 4 billets par match",["en"]="Max 4 tickets per match" },
        ["home_feat_hotel"]       = new(){ ["nl"]="Hotel zoeken",              ["fr"]="Recherche d'hôtel",     ["en"]="Hotel Search" },
        ["home_feat_hotel_sub"]   = new(){ ["nl"]="Vlakbij het stadion",       ["fr"]="Près du stade",         ["en"]="Near the stadium" },

        // ── Stats ─────────────────────────────────────────────────────
        ["stat_clubs"]     = new(){ ["nl"]="Clubs",           ["fr"]="Clubs",         ["en"]="Clubs" },
        ["stat_matches"]   = new(){ ["nl"]="Wedstrijden",     ["fr"]="Matchs",        ["en"]="Matches" },
        ["stat_seats"]     = new(){ ["nl"]="Totaal plaatsen", ["fr"]="Places totales", ["en"]="Total seats" },
        ["stat_languages"] = new(){ ["nl"]="Talen",           ["fr"]="Langues",       ["en"]="Languages" },

        // ── Club card ─────────────────────────────────────────────────
        ["club_view_fixtures"] = new(){ ["nl"]="Bekijk wedstrijden", ["fr"]="Voir les matchs",  ["en"]="View fixtures" },
        ["club_seats"]         = new(){ ["nl"]="plaatsen",           ["fr"]="places",           ["en"]="seats" },
        ["club_col_sector"]    = new(){ ["nl"]="Sector",             ["fr"]="Secteur",          ["en"]="Sector" },
        ["club_col_seats"]     = new(){ ["nl"]="Plaatsen",           ["fr"]="Places",           ["en"]="Seats" },
        ["club_col_from"]      = new(){ ["nl"]="Vanaf €",            ["fr"]="À partir de €",    ["en"]="From €" },

        // ── Match calendar ────────────────────────────────────────────
        ["match_calendar_title"] = new(){
            ["nl"]="Wedstrijdkalender", ["fr"]="Calendrier des matchs", ["en"]="Match Calendar" },
        ["match_calendar_sub"] = new(){
            ["nl"]="Alle Champions League-wedstrijden — klik op Tickets om te kopen",
            ["fr"]="Tous les matchs de la Ligue des Champions — cliquez sur Billets",
            ["en"]="All Champions League fixtures — click Tickets to buy" },
        ["match_btn_tickets"]  = new(){ ["nl"]="Tickets",   ["fr"]="Billets",   ["en"]="Tickets" },
        ["match_sale_open"]    = new(){ ["nl"]="Open",      ["fr"]="Ouvert",    ["en"]="Open" },
        ["match_sale_closed"]  = new(){ ["nl"]="Gesloten",  ["fr"]="Fermé",     ["en"]="Closed" },
        ["match_filter_clear"] = new(){ ["nl"]="Filter wissen", ["fr"]="Effacer le filtre", ["en"]="Clear filter" },
        ["match_col_date"]     = new(){ ["nl"]="Datum",     ["fr"]="Date",      ["en"]="Date" },
        ["match_col_phase"]    = new(){ ["nl"]="Fase",      ["fr"]="Phase",     ["en"]="Phase" },
        ["match_col_home"]     = new(){ ["nl"]="Thuis",     ["fr"]="Domicile",  ["en"]="Home" },
        ["match_col_away"]     = new(){ ["nl"]="Uit",       ["fr"]="Extérieur", ["en"]="Away" },
        ["match_col_stadium"]  = new(){ ["nl"]="Stadion",   ["fr"]="Stade",     ["en"]="Stadium" },
        ["match_col_city"]     = new(){ ["nl"]="Stad",      ["fr"]="Ville",     ["en"]="City" },
        ["match_col_sale"]     = new(){ ["nl"]="Verkoop",   ["fr"]="Vente",     ["en"]="Sale" },

        // ── Match detail ──────────────────────────────────────────────
        ["detail_select_sector"] = new(){ ["nl"]="Kies uw sector",    ["fr"]="Choisissez votre secteur", ["en"]="Select Your Sector" },
        ["detail_home"]          = new(){ ["nl"]="Thuis",             ["fr"]="Domicile",   ["en"]="Home" },
        ["detail_away"]          = new(){ ["nl"]="Uit",               ["fr"]="Extérieur",  ["en"]="Away" },
        ["detail_available"]     = new(){ ["nl"]="Beschikbaar",       ["fr"]="Disponible", ["en"]="Available" },
        ["detail_per_ticket"]    = new(){ ["nl"]="per ticket",         ["fr"]="par billet", ["en"]="per ticket" },
        ["detail_add_cart"]      = new(){ ["nl"]="Voeg toe",          ["fr"]="Ajouter",    ["en"]="Add to cart" },
        ["detail_sold_out"]      = new(){ ["nl"]="Uitverkocht",       ["fr"]="Épuisé",     ["en"]="Sold out" },
        ["detail_sale_closed"]   = new(){ ["nl"]="Ticketverkoop nog niet geopend.", ["fr"]="La vente de billets n'est pas encore ouverte.", ["en"]="Ticket sale is not open yet." },
        ["detail_sale_opens"]    = new(){ ["nl"]="Verkoop opent op",  ["fr"]="Vente ouvre le", ["en"]="Sale opens on" },
        ["detail_find_hotels"]   = new(){ ["nl"]="Hotels zoeken in",  ["fr"]="Trouver des hôtels à", ["en"]="Find hotels in" },
        ["detail_view_cart"]     = new(){ ["nl"]="Bekijk winkelwagen",["fr"]="Voir le panier", ["en"]="View cart" },
        ["detail_added"]         = new(){ ["nl"]="Toegevoegd aan winkelwagen!", ["fr"]="Ajouté au panier!", ["en"]="Added to cart!" },
        ["detail_ticket_s"]      = new(){ ["nl"]="ticket",            ["fr"]="billet",     ["en"]="ticket" },
        ["detail_tickets_pl"]    = new(){ ["nl"]="tickets",           ["fr"]="billets",    ["en"]="tickets" },
        ["detail_match_past"]    = new(){ ["nl"] = "Deze wedstrijd heeft al plaatsgevonden. Tickets zijn niet meer beschikbaar.", ["fr"] = "Ce match a déjà eu lieu. Les billets ne sont plus disponibles.", ["en"] = "This match has already taken place. Tickets are no longer available."},

        // ── Cart ──────────────────────────────────────────────────────
        ["cart_title"]        = new(){ ["nl"]="Winkelwagen",       ["fr"]="Panier",              ["en"]="Shopping Cart" },
        ["cart_empty"]        = new(){ ["nl"]="Uw winkelwagen is leeg.", ["fr"]="Votre panier est vide.", ["en"]="Your cart is empty." },
        ["cart_browse"]       = new(){ ["nl"]="Bekijk beschikbare wedstrijden", ["fr"]="Parcourir les matchs disponibles", ["en"]="Browse available matches" },
        ["cart_col_match"]    = new(){ ["nl"]="Wedstrijd",  ["fr"]="Match",       ["en"]="Match" },
        ["cart_col_date"]     = new(){ ["nl"]="Datum",      ["fr"]="Date",        ["en"]="Date" },
        ["cart_col_sector"]   = new(){ ["nl"]="Sector",     ["fr"]="Secteur",     ["en"]="Sector" },
        ["cart_col_qty"]      = new(){ ["nl"]="Aantal",     ["fr"]="Quantité",    ["en"]="Qty" },
        ["cart_col_unit"]     = new(){ ["nl"]="Prijs/stuk", ["fr"]="Prix unit.",  ["en"]="Unit price" },
        ["cart_col_subtotal"] = new(){ ["nl"]="Subtotaal",  ["fr"]="Sous-total",  ["en"]="Subtotal" },
        ["cart_total"]        = new(){ ["nl"]="Totaal:",    ["fr"]="Total:",      ["en"]="Total:" },
        ["cart_clear"]        = new(){ ["nl"]="Wis winkelwagen", ["fr"]="Vider le panier", ["en"]="Clear cart" },
        ["cart_continue"]     = new(){ ["nl"]="Verder winkelen",  ["fr"]="Continuer les achats", ["en"]="Continue shopping" },
        ["cart_checkout"]     = new(){ ["nl"]="Afrekenen",  ["fr"]="Commander",   ["en"]="Checkout" },
        ["cart_confirm_clear"]= new(){ ["nl"]="Wis de volledige winkelwagen?", ["fr"]="Vider entièrement le panier?", ["en"]="Clear the entire cart?" },

        // ── Checkout ──────────────────────────────────────────────────
        ["checkout_review_title"]    = new(){ ["nl"]="Bestelling controleren",   ["fr"]="Vérifier la commande",  ["en"]="Review Your Order" },
        ["checkout_grand_total"]     = new(){ ["nl"]="Totaalbedrag:",            ["fr"]="Montant total:",        ["en"]="Grand Total:" },
        ["checkout_payment_note"]    = new(){
            ["nl"]="Betaling is gesimuleerd. Vouchers worden naar uw e-mailadres gestuurd.",
            ["fr"]="Le paiement est simulé. Les bons seront envoyés à votre adresse e-mail.",
            ["en"]="Payment is simulated. Vouchers will be sent to your registered email address." },
        ["checkout_confirm"]         = new(){ ["nl"]="Bevestigen & Betalen",     ["fr"]="Confirmer & Payer",     ["en"]="Confirm & Pay" },
        ["checkout_back_cart"]       = new(){ ["nl"]="Terug naar winkelwagen",   ["fr"]="Retour au panier",      ["en"]="Back to cart" },
        ["checkout_confirmed_title"] = new(){ ["nl"]="Aankoop bevestigd!",       ["fr"]="Achat confirmé!",       ["en"]="Purchase Confirmed!" },
        ["checkout_check_email"]     = new(){
            ["nl"]="Controleer uw inbox voor uw vouchercodes.",
            ["fr"]="Vérifiez votre boîte de réception pour vos codes de bon.",
            ["en"]="Check your inbox for your voucher codes." },
        ["checkout_view_tickets"]    = new(){ ["nl"]="Bekijk mijn tickets",      ["fr"]="Voir mes billets",      ["en"]="View My Tickets" },
        ["checkout_browse_more"]     = new(){ ["nl"]="Meer wedstrijden",         ["fr"]="Plus de matchs",        ["en"]="Browse More Matches" },
        ["checkout_col_match"]       = new(){ ["nl"]="Wedstrijd", ["fr"]="Match",    ["en"]="Match" },
        ["checkout_col_sector"]      = new(){ ["nl"]="Sector",    ["fr"]="Secteur",  ["en"]="Sector" },
        ["checkout_col_qty"]         = new(){ ["nl"]="Aantal",    ["fr"]="Quantité", ["en"]="Qty" },
        ["checkout_col_subtotal"]    = new(){ ["nl"]="Subtotaal", ["fr"]="Sous-total",["en"]="Subtotal" },

        // ── My Tickets ────────────────────────────────────────────────
        ["tickets_title"]          = new(){ ["nl"]="Mijn tickets",       ["fr"]="Mes billets",          ["en"]="My Tickets" },
        ["tickets_empty"]          = new(){
            ["nl"]="U heeft nog geen tickets. Zoek een wedstrijd en koop uw eerste ticket!",
            ["fr"]="Vous n'avez pas encore de billets. Trouvez un match et achetez votre premier billet!",
            ["en"]="You have no tickets yet. Find a match and buy your first ticket!" },
        ["tickets_col_match"]      = new(){ ["nl"]="Wedstrijd",  ["fr"]="Match",     ["en"]="Match" },
        ["tickets_col_date"]       = new(){ ["nl"]="Datum",      ["fr"]="Date",      ["en"]="Date" },
        ["tickets_col_sector"]     = new(){ ["nl"]="Sector",     ["fr"]="Secteur",   ["en"]="Sector" },
        ["tickets_col_seat"]       = new(){ ["nl"]="Zitplaats",  ["fr"]="Siège",     ["en"]="Seat" },
        ["tickets_col_price"]      = new(){ ["nl"]="Prijs",      ["fr"]="Prix",      ["en"]="Price paid" },
        ["tickets_col_status"]     = new(){ ["nl"]="Status",     ["fr"]="Statut",    ["en"]="Status" },
        ["tickets_col_voucher"]    = new(){ ["nl"]="Voucher",    ["fr"]="Bon",       ["en"]="Voucher" },
        ["tickets_cancel_btn"]     = new(){ ["nl"]="Annuleren",  ["fr"]="Annuler",   ["en"]="Cancel ticket" },
        ["tickets_cancel_confirm"] = new(){
            ["nl"]="Weet u zeker dat u dit ticket wilt annuleren?",
            ["fr"]="Êtes-vous sûr de vouloir annuler ce billet?",
            ["en"]="Are you sure you want to cancel this ticket? This cannot be undone." },
        ["tickets_not_cancellable"]= new(){ ["nl"]="Niet meer annuleerbaar", ["fr"]="Non annulable", ["en"]="Not cancellable" },
        ["season_tickets_title"] = new() { ["nl"] = "Abonnementen", ["fr"] = "Abonnements", ["en"] = "Season Tickets" },
        ["season_col_club"] = new() { ["nl"] = "Club", ["fr"] = "Club", ["en"] = "Club" },
        ["season_col_purchased"] = new() { ["nl"] = "Aankoopdatum", ["fr"] = "Date d'achat", ["en"] = "Purchase Date" },
        ["season_status_active"] = new() { ["nl"] = "Actief", ["fr"] = "Actif", ["en"] = "Active" },
        ["season_status_cancelled"] = new() { ["nl"] = "Geannuleerd", ["fr"] = "Annulé", ["en"] = "Cancelled" },
        ["ticket_status_paid"] = new() { ["nl"] = "Betaald", ["fr"] = "Payé", ["en"] = "Paid" },
        ["ticket_status_cancelled"] = new() { ["nl"] = "Geannuleerd", ["fr"] = "Annulé", ["en"] = "Cancelled" },
        ["ticket_status_pending"] = new() { ["nl"] = "In behandeling", ["fr"] = "En attente", ["en"] = "Pending" },
        ["tickets_regular_title"] = new() { ["nl"] = "Losse tickets", ["fr"] = "Billets individuels", ["en"] = "Individual Tickets" },

        // ── Login ─────────────────────────────────────────────────────
        ["login_title"]        = new(){ ["nl"]="Inloggen",         ["fr"]="Connexion",      ["en"]="Login" },
        ["login_subtitle"]     = new(){ ["nl"]="Toegang tot uw tickets", ["fr"]="Accédez à vos billets", ["en"]="Access your tickets" },
        ["login_email"]        = new(){ ["nl"]="E-mailadres",      ["fr"]="Adresse e-mail", ["en"]="Email" },
        ["login_password"]     = new(){ ["nl"]="Wachtwoord",       ["fr"]="Mot de passe",   ["en"]="Password" },
        ["login_remember"]     = new(){ ["nl"]="Onthoud mij",      ["fr"]="Se souvenir",    ["en"]="Remember me" },
        ["login_btn"]          = new(){ ["nl"]="Inloggen →",       ["fr"]="Connexion →",    ["en"]="Login →" },
        ["login_no_account"]   = new(){ ["nl"]="Nog geen account?",["fr"]="Pas encore de compte?", ["en"]="No account yet?" },
        ["login_register_link"]= new(){ ["nl"]="Registreer hier",  ["fr"]="Inscrivez-vous ici", ["en"]="Register here" },

        // ── Register ──────────────────────────────────────────────────
        ["reg_title"]        = new(){ ["nl"]="Account aanmaken",    ["fr"]="Créer un compte",     ["en"]="Create Your Account" },
        ["reg_subtitle"]     = new(){ ["nl"]="Tickets kopen in seconden", ["fr"]="Achetez des billets en quelques secondes", ["en"]="Buy tickets in seconds" },
        ["reg_firstname"]    = new(){ ["nl"]="Voornaam",            ["fr"]="Prénom",              ["en"]="First name" },
        ["reg_lastname"]     = new(){ ["nl"]="Achternaam",          ["fr"]="Nom de famille",      ["en"]="Last name" },
        ["reg_email"]        = new(){ ["nl"]="E-mailadres",         ["fr"]="Adresse e-mail",      ["en"]="Email" },
        ["reg_password"]     = new(){ ["nl"]="Wachtwoord",          ["fr"]="Mot de passe",        ["en"]="Password" },
        ["reg_confirm"]      = new(){ ["nl"]="Wachtwoord bevestigen",["fr"]="Confirmer le mot de passe", ["en"]="Confirm password" },
        ["reg_btn"]          = new(){ ["nl"]="Registreren →",       ["fr"]="S'inscrire →",        ["en"]="Register →" },
        ["reg_have_account"] = new(){ ["nl"]="Al een account?",     ["fr"]="Déjà un compte?",     ["en"]="Already have an account?" },
        ["reg_login_link"]   = new(){ ["nl"]="Inloggen hier",       ["fr"]="Connexion ici",       ["en"]="Login here" },

        // ── Forgot / Reset Password ───────────────────────────────────
        ["forgot_title"]              = new(){ ["nl"]="Wachtwoord vergeten",        ["fr"]="Mot de passe oublié",      ["en"]="Forgot Password" },
        ["forgot_subtitle"]           = new(){ ["nl"]="We sturen u een resetlink",  ["fr"]="Nous vous enverrons un lien",["en"]="We will send you a reset link" },
        ["forgot_instructions"]       = new(){
            ["nl"]="Voer uw e-mailadres in. U ontvangt een resetlink als er een account bestaat.",
            ["fr"]="Entrez votre adresse e-mail. Vous recevrez un lien si un compte existe.",
            ["en"]="Enter your email. You will receive a reset link if an account exists." },
        ["forgot_btn"]                = new(){ ["nl"]="Resetlink versturen",        ["fr"]="Envoyer le lien",          ["en"]="Send reset link" },
        ["forgot_back_login"]         = new(){ ["nl"]="Terug naar inloggen",        ["fr"]="Retour a la connexion",    ["en"]="Back to login" },
        ["forgot_confirmation_title"] = new(){ ["nl"]="E-mail verstuurd",           ["fr"]="E-mail envoye",            ["en"]="Email sent" },
        ["forgot_confirmation_text"]  = new(){
            ["nl"]="Controleer uw inbox voor de resetlink. Kijk ook in uw spammap.",
            ["fr"]="Verifiez votre boite de reception. Regardez aussi vos spams.",
            ["en"]="Check your inbox for the reset link. It may also be in your spam folder." },
        ["reset_title"]               = new(){ ["nl"]="Nieuw wachtwoord instellen", ["fr"]="Nouveau mot de passe",     ["en"]="Reset Password" },
        ["reset_btn"]                 = new(){ ["nl"]="Wachtwoord opslaan",         ["fr"]="Enregistrer",             ["en"]="Save password" },
        ["reset_confirmation_title"]  = new(){ ["nl"]="Wachtwoord gewijzigd!",      ["fr"]="Mot de passe modifie!",    ["en"]="Password changed!" },
        ["reset_confirmation_text"]   = new(){
            ["nl"]="U kunt nu inloggen met uw nieuw wachtwoord.",
            ["fr"]="Vous pouvez maintenant vous connecter avec votre nouveau mot de passe.",
            ["en"]="You can now log in with your new password." },

        // ── Register — password requirements & strength ───────────────
        // Identity config: RequiredLength=12, RequireDigit=true,
        // RequireUppercase=true, RequireLowercase=true, RequireNonAlphanumeric=true
        ["reg_pwd_requirements_title"] = new(){ ["nl"]="Wachtwoordvereisten",         ["fr"]="Exigences du mot de passe",          ["en"]="Password requirements" },
        ["reg_pwd_req_length"]         = new(){ ["nl"]="Minimaal 12 tekens",          ["fr"]="Minimum 12 caractères",              ["en"]="At least 12 characters" },
        ["reg_pwd_req_digit"]          = new(){ ["nl"]="Minimaal 1 cijfer (0–9)",     ["fr"]="Au moins 1 chiffre (0–9)",           ["en"]="At least 1 digit (0–9)" },
        ["reg_pwd_req_upper"]          = new(){ ["nl"]="Minimaal 1 hoofdletter (A–Z)",["fr"]="Au moins 1 majuscule (A–Z)",         ["en"]="At least 1 uppercase letter (A–Z)" },
        ["reg_pwd_req_lower"]          = new(){ ["nl"]="Minimaal 1 kleine letter (a–z)",["fr"]="Au moins 1 minuscule (a–z)",       ["en"]="At least 1 lowercase letter (a–z)" },
        ["reg_pwd_req_special"]        = new(){ ["nl"]="Minimaal 1 speciaal teken (!@#$...)",["fr"]="Au moins 1 caractère spécial (!@#$...)",["en"]="At least 1 special character (!@#$...)" },
        ["reg_pwd_req_match"]          = new(){ ["nl"]="Beide wachtwoorden moeten overeenkomen", ["fr"]="Les deux mots de passe doivent correspondre", ["en"]="Both passwords must match" },
        ["reg_pwd_weak"]               = new(){ ["nl"]="Zwak",     ["fr"]="Faible",   ["en"]="Weak" },
        ["reg_pwd_fair"]               = new(){ ["nl"]="Matig",    ["fr"]="Moyen",    ["en"]="Fair" },
        ["reg_pwd_good"]               = new(){ ["nl"]="Goed",     ["fr"]="Bon",      ["en"]="Good" },
        ["reg_pwd_strong"]             = new(){ ["nl"]="Sterk",    ["fr"]="Fort",     ["en"]="Strong" },


        // ── Voucher resend ────────────────────────────────────────────
        ["voucher_resend_btn"]       = new(){ ["nl"]="Voucher opnieuw sturen",    ["fr"]="Renvoyer le bon",         ["en"]="Resend voucher" },
        ["voucher_resend_subject"]   = new(){ ["nl"]="Uw voucher",                ["fr"]="Votre bon",               ["en"]="Your voucher" },
        ["voucher_resend_intro"]     = new(){ ["nl"]="Hierbij uw vouchergegevens voor",
                                              ["fr"]="Voici les détails de votre bon pour",
                                              ["en"]="Here are your voucher details for" },
        ["voucher_resend_footer"]    = new(){ ["nl"]="Toon deze vouchercode aan de ingang van het stadion op wedstrijddag.",
                                              ["fr"]="Présentez ce bon à l entrée du stade le jour du match.",
                                              ["en"]="Present this voucher code at the stadium entrance on match day." },
        ["voucher_resend_success"]   = new(){ ["nl"]="Voucher opnieuw verstuurd naar uw e-mailadres.",
                                              ["fr"]="Bon renvoyé à votre adresse e-mail.",
                                              ["en"]="Voucher resent to your email address." },
        ["voucher_resend_error"]     = new(){ ["nl"]="Ticket niet gevonden.",     ["fr"]="Billet introuvable.",     ["en"]="Ticket not found." },
        ["voucher_resend_cancelled"] = new(){ ["nl"]="Geannuleerde tickets hebben geen geldige voucher.",
                                              ["fr"]="Les billets annulés n ont pas de bon valide.",
                                              ["en"]="Cancelled tickets do not have a valid voucher." },
        ["tickets_cancel_success"]   = new(){ ["nl"]="Ticket succesvol geannuleerd.",
                                              ["fr"]="Billet annulé avec succès.",
                                              ["en"]="Ticket cancelled successfully." },
        // ── Season ticket email ───────────────────────────────────────
        ["season_email_subject"]     = new(){ ["nl"]="Uw abonnement — CL Tickets",
                                              ["fr"]="Votre abonnement— CL Tickets",
                                              ["en"]="Your season ticket — CL Tickets" },
        ["season_email_intro"]       = new(){ ["nl"]="Bedankt voor uw aankoop! Hieronder vindt u de details van uw abonnement.",
                                              ["fr"]="Merci pour votre achat! Voici les détails de votre abonnement.",
                                              ["en"]="Thank you for your purchase! Below are the details of your season ticket." },
        ["season_email_footer"]      = new(){ ["nl"]="Uw abonnement geeft u toegang tot alle thuiswedstrijden van uw club. Bewaar dit e-mailbericht als bewijs van aankoop.",
                                              ["fr"]="Votre abonnement vous donne accès à tous les matchs à domicile de votre club.",
                                              ["en"]="Your season ticket gives you access to all home matches of your club. Keep this email as proof of purchase." },


        ["season_added_to_cart"]        = new(){ ["nl"]="Abonnement toegevoegd aan winkelwagen.",
                                                   ["fr"]="Abonnement ajouté au panier.",
                                                   ["en"]="Season ticket added to cart." },
        ["season_err_already_in_cart"]  = new(){ ["nl"]="Dit vak zit al in uw winkelwagen.",
                                                   ["fr"]="Ce secteur est déjà dans votre panier.",
                                                   ["en"]="This sector is already in your cart." },
        ["season_buy_btn"]              = new(){ ["nl"]="Voeg toe aan winkelwagen",
                                                   ["fr"]="Ajouter au panier",
                                                   ["en"]="Add to cart" },
        ["checkout_confirmed_msg"]      = new(){ ["nl"]="Uw bestelling is bevestigd! Controleer uw inbox voor uw vouchers.",
                                                  ["fr"]="Votre commande est confirmée! Vérifiez votre boîte de réception.",
                                                  ["en"]="Your order is confirmed! Check your inbox for your vouchers." },
        ["cart_col_type"]               = new(){ ["nl"]="Type",            ["fr"]="Type",            ["en"]="Type" },
        ["cart_type_ticket"]            = new(){ ["nl"]="🎟️ Ticket",       ["fr"]="🎟️ Billet",       ["en"]="🎟️ Ticket" },
        ["cart_type_season"]            = new(){ ["nl"]="📅 Abonnement",   ["fr"]="📅 Abonnement",   ["en"]="📅 Season ticket" },
        ["cart_season_title"]           = new(){ ["nl"]="Abonnementen", ["fr"]="Abonnements", ["en"]="Season tickets" },

        // ── Hotel search ──────────────────────────────────────────────
        ["hotel_title"]      = new(){ ["nl"]="Hotels zoeken",      ["fr"]="Rechercher des hôtels", ["en"]="Find Hotels" },
        ["hotel_subtitle"]   = new(){ ["nl"]="Vlakbij het stadion",["fr"]="Près du stade",         ["en"]="Near the Stadium" },
        ["hotel_city"]       = new(){ ["nl"]="Stad",               ["fr"]="Ville",                 ["en"]="City" },
        ["hotel_checkin"]    = new(){ ["nl"]="Inchecken",          ["fr"]="Arrivée",               ["en"]="Check-in" },
        ["hotel_checkout"]   = new(){ ["nl"]="Uitchecken",         ["fr"]="Départ",                ["en"]="Check-out" },
        ["hotel_search_btn"] = new(){ ["nl"]="Zoeken",             ["fr"]="Rechercher",            ["en"]="Search" },
        ["hotel_results_for"]= new(){ ["nl"]="Resultaten voor",    ["fr"]="Résultats pour",        ["en"]="Results for" },
        ["hotel_per_night"]  = new(){ ["nl"]="/ nacht",            ["fr"]="/ nuit",                ["en"]="/ night" },
        ["hotel_book"]       = new(){ ["nl"]="Boek nu →",          ["fr"]="Réserver →",            ["en"]="Book now →" },
        ["hotel_no_results"] = new(){
            ["nl"]="Geen hotels gevonden. Probeer een andere stad of andere datums.",
            ["fr"]="Aucun hôtel trouvé. Essayez une autre ville ou d'autres dates.",
            ["en"]="No hotels found. Try a different city or dates." },

        // ── Season Tickets ────────────────────────────────────────────
        ["season_title"]        = new(){ ["nl"]="Abonnementen",         ["fr"]="Abonnements",         ["en"]="Season Tickets" },
        ["season_subtitle"]     = new(){
            ["nl"]="Koop een abonnement voor alle thuiswedstrijden",
            ["fr"]="Achetez un abonnement pour tous les matchs a domicile",
            ["en"]="Buy a season pass for all home matches" },
        ["season_price"]        = new(){ ["nl"]="Seizoensprijs",                ["fr"]="Prix saison",                ["en"]="Season price" },
        ["season_buy_btn"]      = new(){ ["nl"]="Kopen",                        ["fr"]="Acheter",                    ["en"]="Buy" },
        ["season_info"]         = new(){
            ["nl"]="U kunt nu een abonnement kopen. Abonnementen zijn enkel beschikbaar voor de start van de competitie (22 april 2026). Een abonnementplaats kan daarna niet meer als los ticket verkocht worden.",
            ["fr"]="Vous pouvez acheter un abonnement maintenant. Les abonnements ne sont disponibles qu avant le debut de la competition (22 avril 2026).",
            ["en"]="You can purchase a season ticket now. Season tickets are only available before the competition starts (22 April 2026). A reserved seat cannot be sold as a single ticket after that." },
        ["season_success"]      = new(){
            ["nl"]="Abonnement gekocht! Uw stoelnummer is",
            ["fr"]="Abonnement acheté! Votre numéro de siège est",
            ["en"]="Season ticket purchased! Your seat number is" },
        ["season_err_started"]  = new(){
            ["nl"]="Abonnementen zijn niet meer beschikbaar — de competitie is gestart.",
            ["fr"]="Les abonnements ne sont plus disponibles — la compétition a commencé.",
            ["en"]="Season tickets are no longer available — the competition has started." },
        ["season_err_full"]     = new(){
            ["nl"]="Geen abonnementsplaatsen meer beschikbaar in dit vak.",
            ["fr"]="Plus de places d abonnement disponibles dans ce secteur.",
            ["en"]="No more season ticket seats available in this sector." },
        ["season_closed_title"] = new(){ ["nl"]="Abonnementsverkoop gesloten",  ["fr"]="Vente d abonnements fermee", ["en"]="Season ticket sales closed" },
        ["season_closed_text"]  = new(){
            ["nl"]="De competitie is gestart. Abonnementen zijn niet meer beschikbaar. Koop losse tickets via de wedstrijdkalender.",
            ["fr"]="La competition a commence. Les abonnements ne sont plus disponibles.",
            ["en"]="The competition has started. Season tickets are no longer available. Buy single tickets via the match calendar." },
        // ── Aankoop-foutmeldingen ─────────────────────────────────────
        ["purchase_err_match_not_found"] = new(){
            ["nl"] = "Wedstrijd niet gevonden.",
            ["fr"] = "Match introuvable.",
            ["en"] = "Match not found." },
        ["purchase_err_sale_not_open"] = new(){
            ["nl"] = "Ticketverkoop is nog niet open. Verkoop start op {0}.",
            ["fr"] = "La vente de billets n'est pas encore ouverte. Elle commence le {0}.",
            ["en"] = "Ticket sale is not open. Sale opens on {0}." },
        ["purchase_err_max_tickets"] = new(){
            ["nl"] = "Maximum 4 tickets per persoon per wedstrijd. U heeft al {0} ticket(s). U kunt nog {1} toevoegen.",
            ["fr"] = "Maximum 4 billets par personne par match. Vous avez déjà {0} billet(s). Vous pouvez en ajouter {1}.",
            ["en"] = "Maximum 4 tickets per person per match. You already have {0} ticket(s). You can add {1} more." },
        ["purchase_err_min_quantity"] = new(){
            ["nl"] = "U moet minstens 1 ticket aankopen.",
            ["fr"] = "Vous devez acheter au moins 1 billet.",
            ["en"] = "You must purchase at least 1 ticket." },
        ["purchase_err_same_day"] = new(){
            ["nl"] = "U heeft al een ticket voor een andere wedstrijd op dezelfde dag.",
            ["fr"] = "Vous avez déjà un billet pour un autre match ce jour-là.",
            ["en"] = "You already have a ticket for another match on this day." },
        ["purchase_err_sector_not_found"] = new(){
            ["nl"] = "Sector niet gevonden.",
            ["fr"] = "Secteur introuvable.",
            ["en"] = "Sector not found." },
        ["purchase_err_not_enough_seats"] = new(){
            ["nl"] = "Niet genoeg plaatsen beschikbaar. Nog {0} plaats(en) vrij in dit vak.",
            ["fr"] = "Pas assez de places disponibles. Il reste {0} place(s) dans ce secteur.",
            ["en"] = "Not enough seats available. Only {0} seat(s) left in this sector." },
    };

    /// <summary>Returns the translated UI string for the given key in the current culture.</summary>
    public string T(string key)
    {
        if (_t.TryGetValue(key, out var t))
            if (t.TryGetValue(Culture, out var v))
                return v;
        return key;
    }

    // ── Sector name translations ──────────────────────────────────────────
    // The DB stores sector names in Dutch (the seed language).
    // This dictionary maps each Dutch DB value to FR and EN equivalents.
    private static readonly Dictionary<string, Dictionary<string, string>> _sectors = new()
    {
        ["Onderste ring \u2013 achter doel (thuisploeg)"] = new()
        {
            ["nl"] = "Onderste ring \u2013 achter doel (thuisploeg)",
            ["fr"] = "Anneau inf\u00e9rieur \u2013 derri\u00e8re le but (\u00e9quipe locale)",
            ["en"] = "Lower ring \u2013 behind goal (home side)"
        },
        ["Onderste ring \u2013 achter doel (bezoekers)"] = new()
        {
            ["nl"] = "Onderste ring \u2013 achter doel (bezoekers)",
            ["fr"] = "Anneau inf\u00e9rieur \u2013 derri\u00e8re le but (visiteurs)",
            ["en"] = "Lower ring \u2013 behind goal (away side)"
        },
        ["Onderste ring \u2013 zijlijn Oost"] = new()
        {
            ["nl"] = "Onderste ring \u2013 zijlijn Oost",
            ["fr"] = "Anneau inf\u00e9rieur \u2013 ligne de touche Est",
            ["en"] = "Lower ring \u2013 East sideline"
        },
        ["Onderste ring \u2013 zijlijn West"] = new()
        {
            ["nl"] = "Onderste ring \u2013 zijlijn West",
            ["fr"] = "Anneau inf\u00e9rieur \u2013 ligne de touche Ouest",
            ["en"] = "Lower ring \u2013 West sideline"
        },
        ["Bovenste ring \u2013 achter doel (thuisploeg)"] = new()
        {
            ["nl"] = "Bovenste ring \u2013 achter doel (thuisploeg)",
            ["fr"] = "Anneau sup\u00e9rieur \u2013 derri\u00e8re le but (\u00e9quipe locale)",
            ["en"] = "Upper ring \u2013 behind goal (home side)"
        },
        ["Bovenste ring \u2013 achter doel (bezoekers)"] = new()
        {
            ["nl"] = "Bovenste ring \u2013 achter doel (bezoekers)",
            ["fr"] = "Anneau sup\u00e9rieur \u2013 derri\u00e8re le but (visiteurs)",
            ["en"] = "Upper ring \u2013 behind goal (away side)"
        },
        ["Bovenste ring \u2013 zijlijn Oost"] = new()
        {
            ["nl"] = "Bovenste ring \u2013 zijlijn Oost",
            ["fr"] = "Anneau sup\u00e9rieur \u2013 ligne de touche Est",
            ["en"] = "Upper ring \u2013 East sideline"
        },
        ["Bovenste ring \u2013 zijlijn West"] = new()
        {
            ["nl"] = "Bovenste ring \u2013 zijlijn West",
            ["fr"] = "Anneau sup\u00e9rieur \u2013 ligne de touche Ouest",
            ["en"] = "Upper ring \u2013 West sideline"
        },
    };

    /// <summary>
    /// Translates a sector name stored in Dutch in the database to the
    /// current UI language. Falls back to the Dutch original if not found.
    /// </summary>
    public string TranslateSector(string dutchName)
    {
        if (_sectors.TryGetValue(dutchName, out var translations))
            if (translations.TryGetValue(Culture, out var translated))
                return translated;
        return dutchName;
    }
}
