using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Domain.Models;
    public class ProductCategory : BaseDomainModel
    {
    
    public List<SubCategory> SubCategories;
    public string IconFileName { get; set; }

    }

