using AutoMapper;
using SecureApi.Models;
using SecureApi.Models.DTOs;

namespace SecureApi
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Product, ProductDto>()
          .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()) // Set manually
         .ForMember(dest => dest.Image, opt => opt.Ignore())

          .ForMember(dest => dest.DiscountPercentage, opt => opt.Ignore());

            ; // Not mapped from Product
            CreateMap<CategoryDto, Category>()
                .ForMember(dest => dest.Products, opt => opt.Ignore());
                //.ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<Category, CategoryDto>();

            CreateMap<Order, OrderDto>();

            CreateMap<OrderItems, OrderItemsDto>();
        }

    }
}
