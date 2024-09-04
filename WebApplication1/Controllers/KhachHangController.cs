using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Net.Mail;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Helpers;
using WebApplication1.Repository.Interface;
using WebApplication1.Repository.Service;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly EcommerceContext db;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _environment;
        private readonly IMyEmailSender _myEmailSender;

        public KhachHangController(EcommerceContext context, IMapper mapper, IWebHostEnvironment environment, IMyEmailSender myEmailSender)
        {
            db = context;
            _mapper = mapper;
            _environment = environment;
            _myEmailSender = myEmailSender;
        }

        #region Register
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DangKy(RegisterVM model, IFormFile Hinh)
        {
            if (ModelState.IsValid)
            {
                var checkMakh = db.KhachHangs.SingleOrDefault(c => c.MaKh == model.MaKh || c.Email == model.Email);
                if (checkMakh == null)
                {
                    var khachHang = _mapper.Map<KhachHang>(model);
                    khachHang.RandomKey = MyUtil.GenerateRandomKey();
                    khachHang.MatKhau = model.MatKhau;
                    khachHang.HashMatKhau = model.MatKhau.ToMd5Hash(khachHang.RandomKey);
                    khachHang.HieuLuc = true;//sẽ xử lý khi dùng Mail để active
                    khachHang.VaiTro = 0;


                    if (Hinh != null)
                    {
                        khachHang.Hinh = MyUtil.UpLoadHinh(Hinh, "KhachHang");
                    }
                    string path = Path.Combine(_environment.WebRootPath, "Template/Welcome.cshtml");
                    string htmtString = System.IO.File.ReadAllText(path);
                    htmtString = htmtString.Replace("{{Username}}", model.Email);
                    htmtString = htmtString.Replace("{{url}}", "https://localhost:7225/KhachHang/DangNhap");

                    bool status = await _myEmailSender.EmailSendAsync(model.Email, "Đăng kí người dùng mới", htmtString);
                    db.Add(khachHang);
                    await db.SaveChangesAsync();
                    return RedirectToAction("DangNhap", "KhachHang");
                }
                else
                {
                    ModelState.AddModelError("Loi", "Tên đăng nhập hoặc Email này đã được đăng kí");     
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
                        if(khachHang.MatKhau != model.MatKhau)
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

                            // Cấu hình AuthenticationProperties
                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = true, // Giữ cookie sau khi đóng trình duyệt

                                /*ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Thời gian hết hạn cookie.
                                 * Dù người dùng có request hay không thì 7 ngày vẫn sẽ hết và phải đăng nhập lại
                                 * Tùy vào dự án để thiết lập
                                */
                            };

                            await HttpContext.SignInAsync("UserCookies", claimPrincipal, authProperties);

                            if (Url.IsLocalUrl(ReturnURL))
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


        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync("UserCookies");
            return RedirectToAction("Index", "Home");
        }

        #region forgotPasswords
        [HttpGet]
        public IActionResult forgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> forgotPassword(forgotPasswordVM model)
        {
            if(!ModelState.IsValid)
            {
                return View(model);
            }
            
            var user = db.KhachHangs.SingleOrDefault(kh => kh.Email == model.Email);
            if(user == null)
            {
                ModelState.AddModelError("Email", "Email này không tồn tại.");
            }

            var resetCode = MyUtil.GenerateRandomKey();
            user.ResetCode = resetCode;
            await db.SaveChangesAsync();

            var callBackUrl = Url.Action("ConfirmCode", "KhachHang", null, protocol: HttpContext.Request.Scheme);
            await _myEmailSender.EmailSendAsync(model.Email, "Quên mật khẩu", $"Mã xác nhận của bạn là: {resetCode}. Vui lòng xác nhận mã này tại <a href='{callBackUrl}'>đây</a>.");

            ViewBag.Message = "Mã xác nhận đã được gửi tới email của bạn!";
            return View();
        }
        #endregion

        #region Confirm code
        [HttpGet]
        public IActionResult ConfirmCode()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmCode(ConfirmCodeVM model)
        {
            if(ModelState.IsValid)
            {
                var user = db.KhachHangs.SingleOrDefault(kh => kh.ResetCode == model.Code);
                if(user == null)
                {
                    ModelState.AddModelError("Code", "Mã xác nhận không chính xác");
                }

                // cho phép đổi mật khẩu
                user.MatKhau = model.NewPassWord;
                user.HashMatKhau = model.NewPassWord.ToMd5Hash(user.RandomKey);
                user.ResetCode = null; 
                await db.SaveChangesAsync();

                ViewBag.Message = "Mật khẩu của bạn đã được thay đổi thành công.";
                return RedirectToAction("DangNhap","Khachhang");
            }
            return View(model);
        }
        #endregion
    }
}
