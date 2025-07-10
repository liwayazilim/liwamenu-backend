using AutoMapper;
using QR_Menu.Domain;
using QR_Menu.Application.Users.DTOs;

namespace QR_Menu.Application.Users;

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