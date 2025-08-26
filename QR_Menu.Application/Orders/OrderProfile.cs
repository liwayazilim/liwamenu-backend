using AutoMapper;
using QR_Menu.Application.Orders;
using QR_Menu.Domain;

namespace QR_Menu.Application.Orders;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderReadDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.RestaurantName, opt => opt.MapFrom(s => s.Restaurant != null ? s.Restaurant.Name : string.Empty))
            .ForMember(d => d.TotalAmount, opt => opt.MapFrom(s => s.Items != null ? s.Items.Sum(i => i.LineTotal) : 0))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));

        CreateMap<OrderItem, OrderItemReadDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.ProductNameSnapshot));
    }
} 