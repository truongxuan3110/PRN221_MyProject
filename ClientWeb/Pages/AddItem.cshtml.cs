using DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ClientWeb.Pages
{
    public class AddItemModel : PageModel
    {
        private readonly PRN221_ProjectContext _context;
        public List<ItemType> itemTypes { get; set; } = new List<ItemType>();
        [BindProperty]
        public int SelectedItemType { get; set; }
        [BindProperty]
        public string txtItemName { get; set; }
        [BindProperty]
        public string txtItemDescription { get; set; }
        [BindProperty]
        public int txtMinimumBidIncrement { get; set; }
        [BindProperty]
        public DateTime txtEndDateTime { get; set; }
        public Member user = new Member();
        public AddItemModel(PRN221_ProjectContext context)
        {
            _context = context;
            itemTypes = _context.ItemTypes.ToList();
        }
        public void OnGet()
        {
        }
        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            int? userId = HttpContext.Session.GetInt32("memberId");
            user = _context.Members.SingleOrDefault(x => x.MemberId == userId);
            Item newItem = new Item();
            newItem.ItemId = 0;
            newItem.ItemName = txtItemName;
            newItem.ItemDescription = txtItemDescription;
            newItem.MinimumBidIncrement = txtMinimumBidIncrement;
            newItem.EndDateTime = txtEndDateTime;
            newItem.CurrentPrice = 0;
            newItem.SellerId = user.MemberId;
            newItem.ItemTypeId = SelectedItemType;
            _context.Items.Add(newItem);
            _context.SaveChanges();

            int newItemId = newItem.ItemId;
            Bid bid = new Bid
            {
                ItemId = newItemId,
                BidderId = newItem.SellerId,
                BidDateTime = DateTime.Now,
                BidPrice = newItem.CurrentPrice
            };

            _context.Bids.Add(bid);
            _context.SaveChanges(true);


            return RedirectToPage("/ListAllItems");
        }
    }
}
