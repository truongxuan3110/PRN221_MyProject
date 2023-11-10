using DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ClientWeb.Pages
{
    public class ListBidsModel : PageModel
    {
        private readonly PRN221_ProjectContext _context;
        public List<Bid> bids { get; set; } = new List<Bid>();
        [BindProperty]
        public string item { get; set; } = "";
        public Member user = new Member();
        public ListBidsModel(PRN221_ProjectContext context)
        {
            _context = context;
        }
        public void OnGet(string item)
        {
            int? userId = HttpContext.Session.GetInt32("memberId");
            user = _context.Members.SingleOrDefault(x => x.MemberId == userId);
            if (!string.IsNullOrEmpty(item))
            {
                bids = _context.Bids.Include(x => x.Item).Where(x => x.BidderId == user.MemberId && x.Item.ItemName.Contains(item) && x.BidPrice > 0).ToList();
                ViewData["item"] = item;
            }
            else
            {
                bids = _context.Bids.Include(x => x.Item).Where(x => x.BidderId == user.MemberId && x.BidPrice > 0).ToList();
            }
        }
    }
}
