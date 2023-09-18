using System.Text;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OperationCHAN.Areas.Identity.Pages;

public class Privacy : PageModel
{
    public string PrivacyPolicy;
    
    public async Task<IActionResult> OnGetAsync()
    {
        string content = System.IO.File.ReadAllText("wwwroot/txt/policy.md", Encoding.UTF8);
        PrivacyPolicy = Markdown.ToHtml(content);
        return Page();
    }
}