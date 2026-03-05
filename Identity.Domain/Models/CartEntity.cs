using System.ComponentModel.DataAnnotations;

namespace MyApp.Domain.Models;
    public class CartEntity
    {

        public int Id { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }


         public string ApplicationUserId { get; set; } //unqiue

        public List<CartProduct> Products { get; set; }
       

}

