using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebService.Asmx
{
    public class ProductModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public decimal Price { get; set; }
    }
}