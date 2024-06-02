using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using WebApplication1.Data;
using WebApplication1.Helpers;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(AuthenticationSchemes = "UserCookies", Policy = "RequireUserRole")]
    public class CartController : Controller
    {
        private readonly PaypalClient _paypalClient;
        private readonly EcommerceContext db;

        public CartController(EcommerceContext context, PaypalClient paypalClient)
        {
            _paypalClient = paypalClient;
            db = context;
        }
        public List<CartVM> Cart => HttpContext.Session.Get<List<CartVM>>(MySetting.CART_KEY) ?? new List<CartVM>();
        public IActionResult Index()
        {
            return View(Cart);
        }
        public IActionResult AddToCart(int? id, int quantity = 1)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHH == id);

            if (item == null)
            {
                var hangHoa = db.HangHoas.SingleOrDefault(p => p.MaHh == id);
                if(hangHoa == null)
                {
                    TempData["Message"] = $"Không Tìm Thấy Hang Hóa {id}";
                    return Redirect("/404");
                }
                item = new CartVM
                {
                    MaHH = hangHoa.MaHh,
                    Img = hangHoa.Hinh ?? string.Empty,
                    TenHH = hangHoa.TenHh,
                    DonGia = hangHoa.DonGia ?? 0,
                    Soluong = quantity
                };
                gioHang.Add(item);

            }
            else
            {
                item.Soluong += quantity;
            }
            HttpContext.Session.Set(MySetting.CART_KEY, gioHang);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateCart(int id, int quantity)
        {
            // Thực hiện cập nhật giỏ hàng trên server dựa trên id, quantity và price
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHH == id);

            if (item != null)
            {
                item.Soluong = quantity;
            }

            HttpContext.Session.Set(MySetting.CART_KEY, gioHang);

            // Trả về phản hồi cho client nếu cần
            return RedirectToAction("Index", "Cart");
        }

        public IActionResult Remove(int id)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHH == id);

            if (item != null)
            {  
                gioHang.Remove(item);
            }
            HttpContext.Session.Set(MySetting.CART_KEY, gioHang);
            return RedirectToAction("Index", "Cart");
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            if(Cart.Count == 0)
            {
                return Redirect("/");
            }
            ViewBag.PaypalClientId = _paypalClient.ClientId;
            return View(Cart);
        }

        [HttpPost]
        public IActionResult Checkout(CheckoutVM model)
        {
            if (ModelState.IsValid)
            {
                var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CART_CustomerID).Value;
                var khachHang = new KhachHang();
                if(model.GiongKhachHang)
                {
                    khachHang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == customerId);
                }
                var hoadon = new HoaDon
                {
                    MaKh = customerId,
                    HoTen = model.HoTen ?? khachHang.HoTen,
                    DiaChi = model.DiaChi ?? khachHang.DiaChi,
                    DienThoai = model.DienThoai ?? khachHang.DienThoai,
                    NgayDat = DateTime.Now,
                    NgayGiao = DateTime.Now.AddDays(3),
                    CachThanhToan = "COD",
                    CachVanChuyen = "GRAD",
                    MaTrangThai = 0,
                    NgayCapNhat = DateTime.Now,
                    GhiChu = model.GhiChu,
                };

                // Dòng này bắt đầu một giao dịch cơ sở dữ liệu mới.
                // Mọi thay đổi được thực hiện sau đó trong phạm vi của giao dịch này sẽ không được áp dụng cho cơ sở dữ liệu cho đến khi giao dịch được xác nhận (commit) hoặc hủy bỏ (rollback).
                db.Database.BeginTransaction();
                try
                {
                    // Nếu không có ngoại lệ nào xảy ra trong khối try, giao dịch sẽ được xác nhận và các thay đổi sẽ được áp dụng vào cơ sở dữ liệu
                    db.Database.CommitTransaction();
                    db.Add(hoadon);
                    db.SaveChanges();

                    var cthd = new List<ChiTietHd>();
                    foreach(var item in Cart)
                    {
                        cthd.Add(new ChiTietHd
                        {
                            MaHd = hoadon.MaHd,
                            SoLuong = item.Soluong,
                            DonGia = item.DonGia,
                            MaHh = item.MaHH,
                            GiamGia = 0,

                        });
                    }
                    db.AddRange(cthd);
                    db.SaveChanges();

                    // khi thanh toán thành công thì xét lại danh sách trong giỏ hàng bằng rỗng
                    HttpContext.Session.Set<List<CartVM>>(MySetting.CART_KEY, new List<CartVM>());

                    return View("CheckoutSuccess");
                }
                catch
                {
                    // Nếu có bất kỳ ngoại lệ nào xảy ra trong quá trình thêm hóa đơn hoặc lưu trữ thay đổi vào cơ sở dữ liệu, giao dịch sẽ bị hủy bỏ và mọi thay đổi sẽ không được lưu trữ vào cơ sở dữ liệu
                    db.Database.RollbackTransaction();
                }
            }
            return View(Cart);
        }



        public IActionResult PaymentSuccess()
        {
            return View("CheckoutSuccess");
        }

        #region Paypal payment
        [HttpPost("/Cart/create-paypal-order")]
        public async Task<IActionResult> CreatePaypalOrder(CancellationToken cancellationToken)
        {
            // Thông tin đơn hàng gửi qua Paypal
            var tongTien = Cart.Sum(p => p.ThanhTien).ToString();
            var donViTienTe = "USD";
            var maDonHangThamChieu = "DH" + DateTime.Now.Ticks.ToString();

            try
            {
                var response = await _paypalClient.CreateOrder(tongTien, donViTienTe, maDonHangThamChieu);
                return Ok(response);
            }
            catch(Exception ex)
            {
                var error = new {ex.GetBaseException().Message};
                return BadRequest(error);
            }
        }

        [HttpPost("/Cart/capture-paypal-order")]
        public async Task<IActionResult> CapturePaypalOrder(string orderID,CancellationToken cancellationToken)
        {
            try
            {
                var response = await _paypalClient.CaptureOrder(orderID);

                // lưu database đơn hàng

                return Ok(response);
            }
            catch (Exception ex)
            {
                var error = new { ex.GetBaseException().Message };
                return BadRequest(error);
            }
        }
        #endregion
    }
}
