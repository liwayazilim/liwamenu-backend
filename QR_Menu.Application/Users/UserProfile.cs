using AutoMapper;
using QR_Menu.Domain;
using QR_Menu.Application.Users.DTOs;

namespace QR_Menu.Application.Users;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserReadDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => src.EmailConfirmed))
            .ForMember(dest => dest.CreatedDateTime, opt => opt.MapFrom(src => src.CreatedDateTime))
            .ForMember(dest => dest.UpdatedDateTime, opt => opt.MapFrom(src => src.LastUpdateDateTime))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName));
        CreateMap<UserCreateDto, User>();
        CreateMap<UserUpdateDto, User>();
    }
} 