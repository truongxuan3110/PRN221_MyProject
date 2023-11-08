using System;
using System.Collections.Generic;

namespace DataAccess.Models
{
    public partial class Item
    {
        public Item()
        {
            Bids = new HashSet<Bid>();
        }

        public int ItemId { get; set; }
        public int? ItemTypeId { get; set; }
        public string? ItemName { get; set; }
        public string? ItemDescription { get; set; }
        public int? SellerId { get; set; }
        public int? MinimumBidIncrement { get; set; }
        public DateTime? EndDateTime { get; set; }
        public decimal? CurrentPrice { get; set; }

        public virtual ItemType? ItemType { get; set; }
        public virtual Member? Seller { get; set; }
        public virtual ICollection<Bid> Bids { get; set; }
    }
}
