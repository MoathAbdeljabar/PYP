using MyApp.Application.Product.DTOs.Responses;
using MyApp.Domain.Models;

namespace MyApp.Application.Cart.DTOs.Responses;
    public class CartProductResponse
    {
        public int Id {  get; set; }
        public short Quantity {  get; set; }
        public ProductResponseDto Product { get; set; }
    }

