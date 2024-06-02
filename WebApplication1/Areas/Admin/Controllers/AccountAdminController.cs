using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Helpers;
using WebApplication1.ViewModels;
using AutoMapper;
using WebApplication1.Data;
using WebApplication1.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication1.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountAdminController : Controller
    {
        private readonly EcommerceContext db;
        
        public AccountAdminController(EcommerceContext context)
        {
            db = context;
 
        }
        #region Login
        [HttpGet]
        public IActionResult LoginAdmin(string? ReturnURL)
        {
            ViewBag.ReturnURL = ReturnURL;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoginAdmin(LoginAdminVM model, string? ReturnURL)
        {
            ViewBag.ReturnURL = ReturnURL;
            if (ModelState.IsValid)
            {
                var admin = db.NhanViens.SingleOrDefault(a => a.MaNv == model.Username);
                if (admin == null)
                {
                    ModelState.AddModelError("Loi", "Tài khoản này không tồn tại.");

                }
                else
                {
                        if (admin.MatKhau != model.MatKhau)
                        {
                            ModelState.AddModelError("Loi", "Sai thông tin đăng nhập");
                        }
                        else
                        {
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Email, admin.Email),
                                new Claim(ClaimTypes.Name, admin.HoTen),
                                new Claim(MySetting.CART_CustomerID, admin.MaNv),

                                // claim động để phân quyền
                                new Claim(ClaimTypes.Role, "Admin"),
                            };
                            var claimsIdentity = new ClaimsIdentity(
                                            claims, "AdminCookies");
                            var claimPrincipal = new ClaimsPrincipal(claimsIdentity);

                            await HttpContext.SignInAsync("AdminCookies", claimPrincipal);

                            if (Url.IsLocalUrl(ReturnURL))
                            {
                                return Redirect(ReturnURL);
                            }
                            else
                            {
                                return RedirectToAction("Index", "HomeAdmin");
                            }
                        }
                }
            }
            return View();
        }
        #endregion Login

        public async Task<IActionResult> LogoutAdmin()
        {
            await HttpContext.SignOutAsync("AdminCookies");
            return RedirectToAction("LoginAdmin", "AccountAdmin");
        }
    }
}
