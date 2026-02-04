using System.ComponentModel.DataAnnotations;

namespace MyApp.Domain.Models;
    public class CartProduct
    {
        public int Id { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public short Quantity { get; set; }


        public int CartId { get; set; }
        public int ProductId { get; set; }

        public CartEntity Cart { get; set; } //NP
        public ProductEntity Product { get; set; } //NP



    }

