using AutoMapper;
using RestaurantSystem.Domain;
using RestaurantSystem.Application.Users.DTOs;

namespace RestaurantSystem.Application.Users;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserReadDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
        CreateMap<UserCreateDto, User>();
        CreateMap<UserUpdateDto, User>();
    }
} 