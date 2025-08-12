using AutoMapper;
using QR_Menu.Domain;
using QR_Menu.Application.Products.DTOs;

namespace QR_Menu.Application.Products;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductReadDto>()
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty));
    }
} 