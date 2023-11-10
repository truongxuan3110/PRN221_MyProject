using DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClientWeb.Pages
{
    public class BidDetailModel : PageModel
    {
        private readonly PRN221_ProjectContext _context;
        public Item item { get; set; }
        public Bid bid { get; set; }
        public Member bidder { get; set; }
        public Member seller { get; set; }
        public ItemType itemType { get; set; }
        public int timeRemaining { get; set; }
        [BindProperty]
        public int txtBidPrice { get; set; }
        [BindProperty]
        public string txtItemNumber { get; set; }
        public BidDetailModel(PRN221_ProjectContext context)
        {
            _context = context;
        }
        public void OnGet(int id)
        {
             item = _context.Items.SingleOrDefault(x => x.ItemId == id);
             bid = _context.Bids.SingleOrDefault(x => x.ItemId == id && x.BidPrice == item.CurrentPrice);
             bidder = _context.Members.SingleOrDefault(x => x.MemberId == bid.BidderId);
             seller = _context.Members.SingleOrDefault(x => x.MemberId == item.SellerId);
             itemType = _context.ItemTypes.SingleOrDefault(x => x.ItemTypeId == item.ItemTypeId);
            var time = (Math.Round((item.EndDateTime - DateTime.Now).Value.TotalHours) >= 0) ? Math.Round((item.EndDateTime - DateTime.Now).Value.TotalHours) : 0;
            timeRemaining = int.Parse(time.ToString());
        }
        public IActionResult OnPost()
        {
            Item item = _context.Items.SingleOrDefault(x => x.ItemId == int.Parse(txtItemNumber));
            if (item != null)
            {
                if (Math.Round((item.EndDateTime - DateTime.Now).Value.TotalHours) >= 0)
                {
                    if (txtBidPrice != null)
                    {
                        if (txtBidPrice >= item.CurrentPrice + item.MinimumBidIncrement)
                        {
                            item.CurrentPrice = txtBidPrice;
                            _context.Items.Update(item);

                            Bid bid = new Bid();
                            bid.ItemId = item.ItemId;
                            bid.BidderId = HttpContext.Session.GetInt32("memberId");
                            bid.BidDateTime = DateTime.Now;
                            bid.BidPrice = txtBidPrice;
                            _context.Bids.Add(bid);

                            _context.SaveChanges();
                            TempData["AlertMessage"] = "Bidded successfull";
                        }
                        else
                        {
                            TempData["AlertMessage"] = "Bid Price must be equal to or greater than the 'Current price + Minimum_bid_increment'";
                        }
                    }
                }
                else
                {
                    TempData["AlertMessage"] = "Auction time has expired";
                }
            }
            return RedirectToPage("/BidDetail", new { id = item.ItemId });
        }
    }
}
