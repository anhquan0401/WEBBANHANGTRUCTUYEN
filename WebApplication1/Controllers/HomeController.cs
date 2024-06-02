using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(AuthenticationSchemes = "UserCookies", Policy = "RequireUserRole")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly EcommerceContext db;

        public HomeController(ILogger<HomeController> logger, EcommerceContext context)
        {
            _logger = logger;
            db = context;
        }
        public IActionResult Index(int? loai)
        {
            var hangHoas = db.HangHoas.AsQueryable();
            if(loai.HasValue)
            {
                hangHoas = db.HangHoas.Where(p => p.MaLoai == loai.Value);
            }

            var results = hangHoas.Select(r => new HangHoaVM
            {
                MaHH = r.MaHh,
                TenHH = r.TenHh,
                Soluong = r.SoLuong,
                Img = r.Hinh ?? "",
                DonGia = r.DonGia ?? 0,
                MoTa = r.MoTa ?? ""
            });
            return View(results);
        }

        [Route("/404")]
        public IActionResult PageNotFound()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
