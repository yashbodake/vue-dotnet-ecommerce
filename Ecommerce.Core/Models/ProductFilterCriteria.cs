using System.Collections.Generic;

namespace Ecommerce.Core.Models
{
    public class ProductFilterCriteria
    {
        public IList<int> CategoryIds { get; set; } = new List<int>();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool InStockOnly { get; set; }
        public string SortBy { get; set; } = "name"; // name | price_asc | price_desc | newest
        public string Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
