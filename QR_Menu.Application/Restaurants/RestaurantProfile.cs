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
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.ImageData, opt => opt.Ignore())
            .ForMember(dest => dest.ImageFileName, opt => opt.Ignore())
            .ForMember(dest => dest.ImageContentType, opt => opt.Ignore());
            
        CreateMap<RestaurantUpdateDto, Restaurant>()
            .ForMember(dest => dest.ImageData, opt => opt.Ignore())
            .ForMember(dest => dest.ImageFileName, opt => opt.Ignore())
            .ForMember(dest => dest.ImageContentType, opt => opt.Ignore());
            
        CreateMap<Restaurant, RestaurantUpdateDto>();
    }
} 