using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Reflection.Emit;
using WebApplication1.Data;
using WebApplication1.Helpers;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(AuthenticationSchemes = "UserCookies", Policy = "RequireUserRole")]
    public class ShopController : Controller
    {
        private readonly EcommerceContext db;

        public ShopController(EcommerceContext context)
        {
            db = context;
        }
        public IActionResult Index(int? loai, int page = 1)
        {
            var hangHoas = db.HangHoas.AsQueryable(); // lấy hết danh sách có cùng mã loại

            if (loai.HasValue)
            {
                hangHoas = hangHoas.Where(h => h.MaLoai == loai.Value);
            }

            int pageSize = 9;
            int skip = (page - 1) * pageSize;

            int totalItems = hangHoas.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.Loais = loai;

            var result = hangHoas.Skip(skip).Take(pageSize).Select(r => new HangHoaVM
            {
                MaHH = r.MaHh,
                TenHH = r.TenHh,
                Soluong = r.SoLuong,
                Img = r.Hinh ?? "",
                DonGia = r.DonGia ?? 0,
                MoTa = r.MoTa ?? "",
            }).ToList();

            return View(result); // Trả về danh sách các đối tượng HangHoaVM
        }

        [HttpPost]
        public async Task<IActionResult> Search(string? query)
        {
            var hangHoas = db.HangHoas.AsQueryable(); // lấy hết danh sách có cùng mã loại
            if (query != null)
            {
                hangHoas = hangHoas.Where(h => h.TenHh.Contains(query));
            }

            // kết quả in ra màn hình
            var result = hangHoas.Select(r => new HangHoaVM
            {
                MaHH = r.MaHh,
                TenHH = r.TenHh,
                Soluong = r.SoLuong,
                Img = r.Hinh ?? "",
                DonGia = r.DonGia ?? 0,
                MoTa = r.MoTa ?? ""
            });

            return View(result);
        }
        //[HttpGet]
        //public async Task<IActionResult> SuggestSearch(string? query)
        //{
        //    if (string.IsNullOrWhiteSpace(query))
        //    {
        //        return PartialView("_Suggestion", new List<HangHoa>());
        //    }

        //    var suggestions = db.HangHoas.Where(s => s.TenHh.Contains(query)).ToList();

        //    return PartialView("_Suggestion", suggestions);
        //}

        [HttpGet]
        public IActionResult SuggestSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { });
            }

            var suggestions = db.HangHoas
                .Where(hh => hh.TenHh.Contains(query))
                .Select(hh => new
                {
                    maHh = hh.MaHh,
                    tenHh = hh.TenHh,
                    hinh = hh.Hinh,
                    donGia = hh.DonGia ?? 0
                })
                .Take(5)
                .ToList();

            return Json(suggestions);
        }


        [HttpGet]
        public IActionResult Details(int? id)
        {
            // averageTotal star
            var totalComments = db.Commentss.Where(t => t.MaHh == id).Count();
            var totalRating = db.Commentss.Where(t => t.MaHh == id).Select(p => p.Rating).Sum();
            string averageTotal;

            if (totalComments > 0)
            {
                averageTotal = Math.Round((totalRating / (double)totalComments), 1).ToString();
            }
            else
            {
                averageTotal = "Chưa có đánh giá";
            }
            ViewBag.AverageTotal = averageTotal;

            // total sold
            var sold = db.ChiTietHds.Where(s => s.MaHh == id).Count();
            ViewBag.Sold = sold;

            // deltail
            var data = db.HangHoas
                .Include(p => p.MaLoaiNavigation)
                .Include(p => p.Commentss)
                    .ThenInclude(r => r.MaKhNavigation)
                .SingleOrDefault(p => p.MaHh == id); // dùng SingleOrDefault là lấy hết sản phẩm r nên ko cần gọi data

            if (data == null)
            {
                TempData["Message"] = $"Không thấy sản phẩm {id}";
                return Redirect("/404");
            }

            var result = new ChiTietHangHoaVM
            {
                MaHH = data.MaHh,
                TenHH = data.TenHh,
                Soluong = data.SoLuong,
                TenLoai = data.MaLoaiNavigation.TenLoai,
                Img = data.Hinh,
                DonGia = data.DonGia ?? 0,
                DiemDanhGia = 5, // chech sau
                MoTa = "San Pham",
                ChiTiet = data.MoTa ?? "",
                SoLuongTon = 10, // chech sau
                Commentss = data.Commentss.ToList()
            };
            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> Details(CommentVM model, int id, int Rating, string comment)
        {
            var userID = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CART_CustomerID).Value;
            var results = new Comments
            {
                MaKh = userID,
                MaHh = id,
                CommentDescription = comment,
                Rating = Rating,
                CommentDate = DateTime.Now
            };
            db.Commentss.Add(results);
            await db.SaveChangesAsync();

            return RedirectToAction("Details");
        }



    }
}
