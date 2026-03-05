using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Domain.Models;
    public class ProductImage
    {

    public int Id { get; set; }

    public string StoredName { get; set; }


    public int ProductId { get; set; } //FK
    public ProductEntity Product { get; set; }//NP
    }

