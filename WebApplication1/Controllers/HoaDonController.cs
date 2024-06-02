using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Helpers;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(AuthenticationSchemes = "UserCookies", Policy = "RequireUserRole")]
    public class HoaDonController : Controller
    {
        private readonly EcommerceContext _context;

        public HoaDonController(EcommerceContext context) 
        {
            _context = context;
        }

        public ActionResult LichSuHoaDon() 
        {
            var UserID = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CART_CustomerID).Value;
            var hoaDons = _context.HoaDons
                .Where(hd => hd.MaKh == UserID)
                .Select(hd => new LichSuHoaDonVM
                {
                    MaHd = hd.MaHd,
                    ChiTietHds = hd.ChiTietHds.Select(ct => new ChiTietHdVM
                    {
                        TenHh = ct.MaHhNavigation.TenHh,
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia,
                        GiamGia = ct.GiamGia,
                        Hinh = ct.MaHhNavigation.Hinh,
                        ThanhTien = ct.SoLuong * ct.DonGia,
                    }).ToList()
                }).ToList();
            return View(hoaDons);
        }

        public ActionResult Details(int id)
        {
            var UserID = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CART_CustomerID).Value;
            var hoaDon = _context.HoaDons
                .Where(hd => hd.MaKh == UserID && hd.MaHd == id)
                .Select(hd => new LichSuHoaDonVM
                {
                    MaHd = hd.MaHd,
                    NgayDat = hd.NgayDat,
                    NgayGiao = hd.NgayGiao,
                    HoTen = hd.HoTen,
                    DienThoai = hd.DienThoai,
                    TongTien = hd.ChiTietHds.Sum(ct => ct.DonGia * (ct.SoLuong - ct.GiamGia)),
                    DiaChi = hd.DiaChi,
                    CachThanhToan = hd.CachThanhToan,
                    CachVanChuyen = hd.CachVanChuyen,
                    PhiVanChuyen = hd.PhiVanChuyen,
                    TrangThai = hd.MaTrangThaiNavigation.TenTrangThai,
                    NgayCapNhat = hd.NgayCapNhat,
                    ChiTietHds = hd.ChiTietHds.Select(ct => new ChiTietHdVM
                    {
                        TenHh = ct.MaHhNavigation.TenHh,
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia,
                        GiamGia = ct.GiamGia,
                        Hinh = ct.MaHhNavigation.Hinh,
                        ThanhTien = ct.SoLuong * ct.DonGia,
                    }).ToList()
                }).FirstOrDefault();

            if (hoaDon == null)
            {
                return NotFound();
            }

            return View(hoaDon);
        }

    }
}
