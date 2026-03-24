using AutoMapper;
using FullStackDevelopment_Ticketverkoop.Domain.Entities;
using FullStackDevelopment_Ticketverkoop.Web.Models.ViewModels;

namespace FullStackDevelopment_Ticketverkoop.Web.AutoMapper;

/// <summary>
/// Defines all object-to-object mappings for the web layer.
/// AutoMapper scans for this class at startup via AddAutoMapper(typeof(Program)).
/// Using AutoMapper avoids repetitive manual property assignment code.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Match entity → MatchViewModel for the calendar view
        CreateMap<Match, MatchViewModel>()
            .ForMember(dest => dest.HomeClubName, opt => opt.MapFrom(src => src.HomeClub!.Name))
            .ForMember(dest => dest.AwayClubName, opt => opt.MapFrom(src => src.AwayClub!.Name))
            .ForMember(dest => dest.StadiumName, opt => opt.MapFrom(src => src.HomeClub!.Stadium!.Name))
            .ForMember(dest => dest.HomeLogoUrl, opt => opt.MapFrom(src => src.HomeClub!.LogoUrl));

        // Ticket entity → TicketHistoryViewModel for the order history page
        CreateMap<Ticket, TicketHistoryViewModel>()
            .ForMember(dest => dest.MatchDescription,
                opt => opt.MapFrom(src =>
                    $"{src.Match!.HomeClub!.Name} vs {src.Match.AwayClub!.Name}"))
            .ForMember(dest => dest.MatchDate, opt => opt.MapFrom(src => src.Match!.MatchDate))
            .ForMember(dest => dest.SectionName, opt => opt.MapFrom(src => src.SectionType!.Name));
    }
}