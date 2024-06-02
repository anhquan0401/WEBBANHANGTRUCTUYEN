using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Reflection.Emit;
using WebApplication1.Data;
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

        public IActionResult Search(string? query)
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

        [HttpGet]
        public JsonResult SuggestSearch(string? query)
        {
            //var hangHoas = db.HangHoas.AsQueryable(); // lấy hết danh sách có cùng mã loại
            var hangHoas = db.HangHoas.Where(h => h.TenHh.Contains(query)).Select(h => h.TenHh).ToList();

            return Json(hangHoas);
        }

        public IActionResult Details(int? id)
        {
            var data = db.HangHoas
                .Include(p => p.MaLoaiNavigation)
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
                SoLuongTon = 10 // chech sau
            };
            return View(result);
        }
    }
}
