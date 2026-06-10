using System;
using System.Linq;
using AutoMapper;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.DTOs;
using BusinessManager.Domain.Enums;

namespace BusinessManager.Application.Validators;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        CreateMap<UserDto, User>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => (UserRole)Enum.Parse(typeof(UserRole), src.Role)));

        // ServiceItem mappings
        CreateMap<ServiceItem, ServiceItemDto>()
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.ToString()));

        CreateMap<ServiceItemDto, ServiceItem>()
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => Enum.Parse<ServiceCategory>(src.Category)));

        // Product mappings
        CreateMap<Product, ProductDto>();

        CreateMap<ProductDto, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // Sale mappings
        CreateMap<Sale, SaleDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName));

        CreateMap<SaleDto, Sale>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.SaleItems, opt => opt.Ignore());

        // SaleItem mappings
        CreateMap<SaleItem, SaleItemDto>()
            .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.ServiceItem != null ? src.ServiceItem.Name : null))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null));

        CreateMap<SaleItemDto, SaleItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Sale, opt => opt.Ignore())
            .ForMember(dest => dest.ServiceItem, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore());

        // Expense mappings
        CreateMap<Expense, ExpenseDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.ExpenseCategory.Name))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName));

        CreateMap<ExpenseDto, Expense>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.ExpenseCategory, opt => opt.Ignore());

        // ExpenseCategory mappings
        CreateMap<ExpenseCategory, ExpenseCategoryDto>();

        CreateMap<ExpenseCategoryDto, ExpenseCategory>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Expenses, opt => opt.Ignore());

        // Setting mappings
        CreateMap<Setting, SettingDto>();

        CreateMap<SettingDto, Setting>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }
}

// Additional DTOs for validation
public class ExpenseCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; }
}

public class SettingDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
}
