using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Ascendia.Data;
using Ascendia.Models;

namespace Ascendia.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _dbContext;

        public IndexModel(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<SchoolNotice> Notices { get; private set; } = new();

        public async Task OnGetAsync()
        {
            var todayUtc = DateTime.UtcNow.Date;
            Notices = await _dbContext.Notices
                .AsNoTracking()
                .Where(notice => notice.IsActive
                    && (!notice.ExpiryDateUtc.HasValue || notice.ExpiryDateUtc >= todayUtc))
                .OrderByDescending(notice => notice.IsPinned)
                .ThenByDescending(notice => notice.DatePostedUtc)
                .ToListAsync(HttpContext.RequestAborted);
        }
    }
}