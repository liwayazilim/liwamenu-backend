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
            .ForMember(d => d.ProductsCount, opt => opt.MapFrom(s => s.Products != null ? s.Products.Count : 0))
            .ForMember(d => d.ActiveProductsCount, opt => opt.MapFrom(s => s.Products != null ? s.Products.Count(p => p.IsActive) : 0));
            
        CreateMap<CategoryCreateDto, Category>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Restaurant, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.Description, opt => opt.Ignore())
            .ForMember(dest => dest.ShortDescription, opt => opt.Ignore())
            .ForMember(dest => dest.Color, opt => opt.Ignore())
            .ForMember(dest => dest.DisplayOrder, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.IsMainCategory, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedDateTime, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.LastUpdateDateTime, opt => opt.MapFrom(src => DateTime.UtcNow));
            
        CreateMap<CategoryUpdateDto, Category>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RestaurantId, opt => opt.Ignore())
            .ForMember(dest => dest.Restaurant, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.Description, opt => opt.Ignore())
            .ForMember(dest => dest.ShortDescription, opt => opt.Ignore())
            .ForMember(dest => dest.Color, opt => opt.Ignore())
            .ForMember(dest => dest.DisplayOrder, opt => opt.Ignore())
            .ForMember(dest => dest.IsMainCategory, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDateTime, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdateDateTime, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
} 