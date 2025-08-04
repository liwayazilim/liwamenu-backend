using AutoMapper;
using QR_Menu.Domain;
using QR_Menu.Application.Payments.DTOs;

namespace QR_Menu.Application.Payments;

public class PaymentProfile : Profile
{
    public PaymentProfile()
    {
        CreateMap<Payment, PaymentReadDto>()
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
            .ForMember(dest => dest.RestaurantName, opt => opt.MapFrom(src => src.Restaurant != null ? src.Restaurant.Name : null));
            
        CreateMap<PaymentCreateDto, Payment>()
            .ForMember(dest => dest.OrderNumber, opt => opt.Ignore()) // Will be generated
            .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore()) // Will be set by external payment service
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => PaymentStatus.Waiting))
            .ForMember(dest => dest.CreatedDateTime, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.LastUpdateDateTime, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
} 