using AutoMapper;
using RestaurantSystem.Domain;
using RestaurantSystem.Application.Restaurants.DTOs;

namespace RestaurantSystem.Application.Restaurants;

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