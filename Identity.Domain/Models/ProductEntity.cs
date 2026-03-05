using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyApp.Domain.Enums;


namespace MyApp.Domain.Models;
    public class ProductEntity
    {
        public int Id { get; set; }

        [Length(0,25)]
        public string Name {  get; set; }
        public string Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }


        [Timestamp]
        public byte[] RowVersion { get; set; }


        //Relation with ProductImage  
        public List<ProductImage> ProductImages { get; set; }



        //Relation with SubCategory
        public int SubCategoryId { get; set; }
        public SubCategory SubCategory { get; set; }


        //Relation with User
        public string ApplicationUserId { get; set; }


        public EnProductState ProductState { get; set; }


        public DateTime UpdatedAt { get; set; }

        public string? StateChangedBy { get; set; } = string.Empty;

        public int StockQuantity { get; set; }  


}

