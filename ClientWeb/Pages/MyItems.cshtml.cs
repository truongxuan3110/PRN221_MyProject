using DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ClientWeb.Pages
{
    public class MyItemsModel : PageModel
    {
        private readonly PRN221_ProjectContext _context;
        public List<Item> items { get; set; } = new List<Item>();
        [BindProperty]
        public string item { get; set; } = "";
        public MyItemsModel(PRN221_ProjectContext context)
        {
            _context = context;
        }
        public void OnGet(string item)
        {
            int? userId = HttpContext.Session.GetInt32("memberId");
            if (!string.IsNullOrEmpty(item))
            {
                items = _context.Items.Include(x => x.ItemType).Include(x => x.Seller).Where(x => x.ItemName.Contains(item) && x.SellerId==userId).ToList();
                ViewData["item"] = item;
            }
            else
            {
                items = _context.Items.Include(x => x.ItemType).Include(x => x.Seller).Where(x => x.SellerId == userId).ToList();
            }
        }
    }
}
