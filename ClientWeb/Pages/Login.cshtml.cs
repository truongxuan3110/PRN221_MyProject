using DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClientWeb.Pages
{
    public class LoginModel : PageModel
    {
        private readonly PRN221_ProjectContext _context;
        public LoginModel(PRN221_ProjectContext context)
        {
            _context = context;
        }
        [BindProperty]
        public string username { get; set; }

        [BindProperty]
        public string password { get; set; }
        public void OnGet()
        {
        }
        public IActionResult OnPost()
        {
            Member member = _context.Members.SingleOrDefault(x=>x.Email==username && x.Password==password);
            if (member != null)
            {
                HttpContext.Session.SetInt32("memberId", member.MemberId);
                return RedirectToPage("/ListAllItems");
            }
            ModelState.AddModelError("", "Invalid username or password.");
            return Page();
        }
    }
}
