using AutoMapper;
using QR_Menu.Application.OrderTags;
using QR_Menu.Domain;

namespace QR_Menu.Application.OrderTags;

public class OrderTagProfile : Profile
{
    public OrderTagProfile()
    {
        CreateMap<OrderTag, OrderTagReadDto>();
        CreateMap<OrderTagCreateDto, OrderTag>();
        CreateMap<OrderTagUpdateDto, OrderTag>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
} 