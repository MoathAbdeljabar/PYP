using System.ComponentModel.DataAnnotations;

namespace MyApp.Application.Cart.DTOs.Requests;
    public class UpdateCartItemRequest
    {
        public int CartItemId { get; set; }

        [Range(1, short.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public short Quantity { get; set; }
    }

