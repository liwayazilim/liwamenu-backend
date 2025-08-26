using AutoMapper;
using QR_Menu.Domain;
using QR_Menu.Application.Restaurants.DTOs;

namespace QR_Menu.Application.Restaurants;

public class RestaurantProfile : Profile
{
    public RestaurantProfile()
    {
        CreateMap<Restaurant, RestaurantReadDto>()
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.ImageFileName) ? $"/images/restaurants/{src.ImageFileName}" : null));
            
        CreateMap<RestaurantCreateDto, Restaurant>()
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Latitude))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Longitude))
            .ForMember(dest => dest.ThemeId, opt => opt.MapFrom(src => src.ThemeId ?? 0))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.ImageData, opt => opt.Ignore())
            .ForMember(dest => dest.ImageFileName, opt => opt.Ignore())
            .ForMember(dest => dest.ImageContentType, opt => opt.Ignore());
            
        CreateMap<RestaurantUpdateDto, Restaurant>()
            .ForMember(dest => dest.ImageData, opt => opt.Ignore())
            .ForMember(dest => dest.ImageFileName, opt => opt.Ignore())
            .ForMember(dest => dest.ImageContentType, opt => opt.Ignore());
            
        CreateMap<Restaurant, RestaurantUpdateDto>();
        
        CreateMap<Restaurant, RestaurantSettingsResponseDto>();
        CreateMap<RestaurantSettingsUpdateDto, Restaurant>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.DealerId, opt => opt.Ignore())
            .ForMember(dest => dest.LicenseId, opt => opt.Ignore())
            .ForMember(dest => dest.Name, opt => opt.Ignore())
            .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
            .ForMember(dest => dest.City, opt => opt.Ignore())
            .ForMember(dest => dest.District, opt => opt.Ignore())
            .ForMember(dest => dest.Neighbourhood, opt => opt.Ignore())
            .ForMember(dest => dest.Address, opt => opt.Ignore())
            .ForMember(dest => dest.Latitude, opt => opt.Ignore())
            .ForMember(dest => dest.Longitude, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.WorkingHours, opt => opt.Ignore())
            .ForMember(dest => dest.SocialLinks, opt => opt.Ignore())
            .ForMember(dest => dest.ThemeId, opt => opt.Ignore())
            .ForMember(dest => dest.ImageData, opt => opt.Ignore())
            .ForMember(dest => dest.ImageFileName, opt => opt.Ignore())
            .ForMember(dest => dest.ImageContentType, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDateTime, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdateDateTime, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
} 