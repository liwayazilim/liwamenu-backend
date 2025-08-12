using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QR_Menu.Application.Categories.DTOs;
using QR_Menu.Domain;
using QR_Menu.Infrastructure;

namespace QR_Menu.Application.Categories;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryReadDto>()
            .ForMember(d => d.ProductsCount, opt => opt.MapFrom(s => s.Products != null ? s.Products.Count : 0));
    }
} 