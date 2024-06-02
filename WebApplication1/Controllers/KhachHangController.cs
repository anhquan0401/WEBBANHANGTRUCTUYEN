using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Net.Mail;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Helpers;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly EcommerceContext db;
        private readonly IMapper _mapper;

        public KhachHangController(EcommerceContext context, IMapper mapper)
        {
            db = context;
            _mapper = mapper;
        }

        #region Register
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DangKy(RegisterVM model, IFormFile Hinh)
        {
            if (ModelState.IsValid)
            {
                var checkMakh = db.KhachHangs.SingleOrDefault(c => c.MaKh == model.MaKh);
                if (checkMakh == null)
                {
                    var khachHang = _mapper.Map<KhachHang>(model);
                    khachHang.RandomKey = MyUtil.GenerateRandomKey();
                    khachHang.MatKhau = model.MatKhau.ToMd5Hash(khachHang.RandomKey);
                    khachHang.HieuLuc = true;//sẽ xử lý khi dùng Mail để active
                    khachHang.VaiTro = 0;


                    if (Hinh != null)
                    {
                        khachHang.Hinh = MyUtil.UpLoadHinh(Hinh, "KhachHang");
                    }

                    db.Add(khachHang);
                    db.SaveChanges();
                    return RedirectToAction("DangNhap", "KhachHang");
                }
                else
                {
                    ModelState.AddModelError("Loi", "Tên đăng nhập này đã được đăng kí");     
                }
            }
            return View();
        }
        #endregion Register

        #region Login
        [HttpGet]
        public IActionResult DangNhap(string? ReturnURL)
        {
            ViewBag.ReturnURL = ReturnURL;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DangNhap(LoginVM model, string? ReturnURL)
        {
            ViewBag.ReturnURL = ReturnURL;
            if(ModelState.IsValid)
            {
                var khachHang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == model.Username);
                if(khachHang == null)
                {
                    ModelState.AddModelError("Loi", "Tài khoản này không tồn tại. Vui lòng đăng kí!");

                } 
                else
                {
                    if(!khachHang.HieuLuc)
                    {
                        ModelState.AddModelError("Loi", "Tài khoản này đã bị khóa. Vui lòng đăng kí lại!");
                    }
                    else
                    {
                        if(khachHang.MatKhau != model.MatKhau.ToMd5Hash(khachHang.RandomKey))
                        {
                            ModelState.AddModelError("Loi", "Sai thông tin đăng nhập");
                        }
                        else
                        {
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Email, khachHang.Email),
                                new Claim(ClaimTypes.Name, khachHang.HoTen),
                                new Claim(MySetting.CART_CustomerID, khachHang.MaKh),

                                // claim động để phân quyền
                                new Claim(ClaimTypes.Role, "Customer"),
                            };
                            var claimsIdentity = new ClaimsIdentity(
                                            claims, "UserCookies");
                            var claimPrincipal = new ClaimsPrincipal(claimsIdentity);

                            await HttpContext.SignInAsync("UserCookies", claimPrincipal);

                            if(Url.IsLocalUrl(ReturnURL))
                            {
                                return Redirect(ReturnURL);
                            }
                            else
                            {
                                return Redirect("/");
                            }
                        }
                    }
                }
            }
            return View();
        }
        #endregion Login

        [Authorize(AuthenticationSchemes = "UserCookies", Policy = "RequireUserRole")]
        public IActionResult proFile() {
            return View(); 
        }

        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync("UserCookies");
            return RedirectToAction("Index", "Home");
        }

    }
}
