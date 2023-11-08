using System;
using System.Collections.Generic;

namespace DataAccess.Models
{
    public partial class Bid
    {
        public int BidId { get; set; }
        public int? ItemId { get; set; }
        public int? BidderId { get; set; }
        public DateTime? BidDateTime { get; set; }
        public decimal? BidPrice { get; set; }

        public virtual Member? Bidder { get; set; }
        public virtual Item? Item { get; set; }
    }
}
