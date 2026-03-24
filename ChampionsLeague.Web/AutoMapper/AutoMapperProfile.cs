using AutoMapper;
using ChampionsLeague.Domain.Entities;
using ChampionsLeague.Web.ViewModels;

namespace ChampionsLeague.Web.AutoMapper;

/// <summary>
/// AutoMapper profile — defines all entity → ViewModel mappings in one place.
/// Inheriting from Profile and calling CreateMap() in the constructor is the
/// standard pattern shown in section 10.4 of the curriculum.
/// AutoMapper is registered in Program.cs via AddAutoMapper(typeof(Program)),
/// which scans the assembly for all Profile subclasses automatically.
/// </summary>
public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // Match → MatchListItemVM (calendar list)
        CreateMap<Match, MatchListItemVM>()
            .ForMember(d => d.HomeClubName,  o => o.MapFrom(s => s.HomeClub.Name))
            .ForMember(d => d.AwayClubName,  o => o.MapFrom(s => s.AwayClub.Name))
            .ForMember(d => d.HomeClubBadge, o => o.MapFrom(s => s.HomeClub.BadgeUrl))
            .ForMember(d => d.AwayClubBadge, o => o.MapFrom(s => s.AwayClub.BadgeUrl))
            .ForMember(d => d.StadiumName,   o => o.MapFrom(s => s.HomeClub.Stadium != null ? s.HomeClub.Stadium.Name : ""))
            .ForMember(d => d.StadiumCity,   o => o.MapFrom(s => s.HomeClub.Stadium != null ? s.HomeClub.Stadium.City : ""))
            .ForMember(d => d.IsSaleOpen,    o => o.MapFrom(s => s.IsSaleOpen));

        // Club → ClubCardVM (home page cards)
        CreateMap<Club, ClubCardVM>()
            .ForMember(d => d.StadiumName,   o => o.MapFrom(s => s.Stadium != null ? s.Stadium.Name : ""))
            .ForMember(d => d.StadiumCity,   o => o.MapFrom(s => s.Stadium != null ? s.Stadium.City : ""))
            .ForMember(d => d.TotalCapacity, o => o.MapFrom(s =>
                s.Stadium != null ? s.Stadium.Sectors.Sum(sec => sec.Capacity) : 0))
            .ForMember(d => d.Sectors,       o => o.MapFrom(s =>
                s.Stadium != null ? s.Stadium.Sectors : new List<Sector>()));

        // Sector → SectorVM
        CreateMap<Sector, SectorVM>();

        // Ticket → TicketHistoryItemVM (My Tickets page)
        CreateMap<Ticket, TicketHistoryItemVM>()
            .ForMember(d => d.MatchDescription, o => o.MapFrom(s =>
                $"{s.Match.HomeClub.Name} vs {s.Match.AwayClub.Name}"))
            .ForMember(d => d.MatchDate,  o => o.MapFrom(s => s.Match.MatchDate))
            .ForMember(d => d.SectorName, o => o.MapFrom(s => s.Sector.Name))
            .ForMember(d => d.Status,     o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.IsCancellable, o => o.MapFrom(s => s.Match.IsCancellable));
    }
}
