using AutoMapper;
using QR_Menu.Domain;
using QR_Menu.Application.Restaurants.DTOs;

namespace QR_Menu.Application.Restaurants;

public class RestaurantProfile : Profile
{
    public RestaurantProfile()
    {
        CreateMap<Restaurant, RestaurantReadDto>();
        CreateMap<RestaurantCreateDto, Restaurant>();
        CreateMap<RestaurantUpdateDto, Restaurant>();
        CreateMap<Restaurant, RestaurantUpdateDto>();
    }
} 