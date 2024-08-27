using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using System.IdentityModel.Tokens.Jwt;
using WebApplicationUI.Models;
using WebApplicationUI.Service.IService;
using WebApplicationUI.SignalR;
using WebApplicationUI.Utility;
namespace WebApplicationUI.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly PayPalHttpClient _payPalClient;
        private readonly IHubContext<CartHub> _hubContext;
        public CartController(ICartService cartService, IOrderService orderService,
            IProductService productService,  PayPalHttpClient payPalHttpClient, IHubContext<CartHub> hubContext)
        {
            _cartService = cartService;
            _orderService = orderService;
            _productService = productService;
            _payPalClient = payPalHttpClient;
            _hubContext=hubContext;
        }
        [Authorize]
        public async Task<IActionResult> CartIndex()
        {
            try
            {
                var data = await LoadCartDtoBasedOnLoggedInUser();
                int cartCount = data.CartDetails != null ? data.CartDetails.Count() : 0;
                HttpContext.Session.SetInt32("CartCount", cartCount);
                // Notify clients about the cart data update.
                await _hubContext.Clients.All.SendAsync("ReceiveCartDataUpdate", data);
                return View(data);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
            }
            return View();
        }
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                CartDto cartDto = await LoadCartDtoBasedOnLoggedInUser();
                //IEnumerable<CartDetailsDto> Cart = cartDto.CartDetails.Where(u => u.Count > 1).ToList();
                //cartDto.CartDetails =null;
                //cartDto.CartDetails = Cart;
                return View(cartDto);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
            }
            return View();
        }
        [HttpPost]
        [ActionName("Checkout")]
        public async Task<IActionResult> Checkout(CartDto cartDto, string paymentMethod)
        {
            try
            {
                CartDto cart = await LoadCartDtoBasedOnLoggedInUser();
                cart.CartHeader.Phone = cartDto.CartHeader.Phone;
                cart.CartHeader.Email = cartDto.CartHeader.Email;
                cart.CartHeader.Name = cartDto.CartHeader.Name;
                var response = await _orderService.CreateOrder(cart);
                OrderHeaderDto orderHeaderDto = JsonConvert.DeserializeObject<OrderHeaderDto>(Convert.ToString(response.Result));

                if (response != null && response.IsSuccess)
                {
                    var domain = Request.Scheme + "://" + Request.Host.Value + "/";
                    if (paymentMethod == "stripe")
                    {                      
                        StripeRequestDto stripeRequestDto = new StripeRequestDto
                        {
                            ApprovedUrl = domain + "cart/Confirmation?orderId=" + orderHeaderDto.OrderHeaderId,
                            CancelUrl = domain + "cart/checkout",
                            OrderHeader = orderHeaderDto
                        };
                        var stripeResponse = await _orderService.CreateStripeSession(stripeRequestDto);
                        StripeRequestDto stripeResponseResult = JsonConvert.DeserializeObject<StripeRequestDto>(Convert.ToString(stripeResponse.Result));
                        // Redirect user to Stripe checkout page
                        Response.Headers.Add("Location", stripeResponseResult.StripeSessionUrl);
                        return new StatusCodeResult(303);
                    }
                    else if (paymentMethod == "paypal")
                    {
                        PayPalRequestDto payPalRequestDto = new PayPalRequestDto
                        {
                            ApprovedUrl = domain + "cart/Confirmationpaypal?orderId=" + orderHeaderDto.OrderHeaderId,
                            CancelUrl = domain + "cart/checkout",
                            OrderHeader = orderHeaderDto
                        };

                        var payPalResponse = await _orderService.CreatePayPalPayment(payPalRequestDto);
                        PayPalRequestDto payPalRequest = JsonConvert.DeserializeObject<PayPalRequestDto>(Convert.ToString(payPalResponse.Result));
                        Response.Headers.Add("Location", payPalRequest.ApprovedUrl);
                        return new StatusCodeResult(303);
                    }
                }
                return View();
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
                return View();
            }
        }
        public async Task<IActionResult> Confirmationpaypal(string token, string PayerID, int orderId)
        {
            try
            {
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(PayerID))
                {
                    var order = await GetPayPalOrder(token);
                }
                ResponseDto? response = await _orderService.ValidatePayPal(orderId);
                if (response != null && response.IsSuccess)
                {
                    OrderHeaderDto orderHeader = JsonConvert.DeserializeObject<OrderHeaderDto>(Convert.ToString(response.Result));
                    if (orderHeader.Status == SD.Status_Approved && orderHeader.OrderDetails != null)
                    {
                        var userId = User.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub)?.Value;
                        foreach (var orderDetail in orderHeader.OrderDetails)
                        {
                            if (orderDetail.ProductId != null)
                            {
                                var productId = orderDetail.ProductId;
                                ResponseDto? responseproduct = await _productService.GetProductByIdAsync(productId);
                                if (responseproduct != null && responseproduct.IsSuccess)
                                {
                                    ProductDto productDto = JsonConvert.DeserializeObject<ProductDto>(Convert.ToString(responseproduct.Result));
                                    if (productDto.Count >= orderDetail.Count)
                                    {
                                        productDto.Count -= orderDetail.Count;
                                        ResponseDto? responsedata = await _productService.UpdateProductsAsync(productDto);

                                    }
                                    ResponseDto? responsecart = await _cartService.GetCartByUserIdAsnyc(userId);
                                    if (responsecart != null && responsecart.IsSuccess)
                                    {
                                        CartDto cartDto = JsonConvert.DeserializeObject<CartDto>(Convert.ToString(responsecart.Result));
                                        foreach (var cartDetail in cartDto.CartDetails)
                                        {
                                            if (cartDetail.CartDetailsId != null)
                                            {
                                                var data = cartDetail.CartDetailsId;
                                                ResponseDto? responseproductcart = await _cartService.RemoveFromCartAsync(data);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return View(response);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task<Order?> GetPayPalOrder(string token)
        {
            try
            {
                var request = new OrdersGetRequest(token);
                var response = await _payPalClient.Execute(request);
                if (response is not null && response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return response.Result<Order>();
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IActionResult> Confirmation(int orderId)
        {
            try
            {
                ResponseDto? response = await _orderService.ValidateStripeSession(orderId);
                if (response != null && response.IsSuccess)
                {
                    OrderHeaderDto orderHeader = JsonConvert.DeserializeObject<OrderHeaderDto>(Convert.ToString(response.Result));
                    if (orderHeader.Status == SD.Status_Approved && orderHeader.OrderDetails != null)
                    {
                        var userId = User.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub)?.Value;
                        foreach (var orderDetail in orderHeader.OrderDetails)
                        {
                            if (orderDetail.ProductId != null)
                            {
                                var productId = orderDetail.ProductId;
                                ResponseDto? responseproduct = await _productService.GetProductByIdAsync(productId);
                                if (responseproduct != null && responseproduct.IsSuccess)
                                {
                                    ProductDto productDto = JsonConvert.DeserializeObject<ProductDto>(Convert.ToString(responseproduct.Result));
                                    if (productDto.Count >= orderDetail.Count)
                                    {
                                        productDto.Count -= orderDetail.Count;
                                        ResponseDto? responsedata = await _productService.UpdateProductsAsync(productDto);

                                    }
                                    ResponseDto? responsecart = await _cartService.GetCartByUserIdAsnyc(userId);
                                    if (responsecart != null && responsecart.IsSuccess)
                                    {
                                        CartDto cartDto = JsonConvert.DeserializeObject<CartDto>(Convert.ToString(responsecart.Result));
                                        foreach (var cartDetail in cartDto.CartDetails)
                                        {
                                            if (cartDetail.CartDetailsId != null)
                                            {
                                                var data = cartDetail.CartDetailsId;
                                                ResponseDto? responseproductcart = await _cartService.RemoveFromCartAsync(data);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return View(response);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
            }
            return View();
        }

        public async Task<IActionResult> Remove(int cartDetailsId)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("RemoveCartData", cartDetailsId);
                var userId = User.Claims.Where(u => u.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
                ResponseDto? response = await _cartService.RemoveFromCartAsync(cartDetailsId);
                if (response != null & response.IsSuccess)
                {
                    TempData["success"] = "Cart updated successfully";
                    return RedirectToAction(nameof(CartIndex));
                }
                return View();
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(CartDto cartDto)
        {
            try
            {
                ResponseDto? response = await _cartService.ApplyCouponAsync(cartDto);
                if (response != null & response.IsSuccess)
                {
                    TempData["success"] = "Cart updated successfully";
                    return RedirectToAction(nameof(CartIndex));
                }
                return View();
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> EmailCart(CartDto cartDto)
        {
            try
            {
                CartDto cart = await LoadCartDtoBasedOnLoggedInUser();
                cart.CartHeader.Email = User.Claims.Where(u => u.Type == JwtRegisteredClaimNames.Email)?.FirstOrDefault()?.Value;
                ResponseDto? response = await _cartService.EmailCart(cart);
                if (response != null & response.IsSuccess)
                {
                    TempData["success"] = "Email will be processed and sent shortly.";
                    return RedirectToAction(nameof(CartIndex));
                }
                return View();
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> RemoveCoupon(CartDto cartDto)
        {
            try
            {
                cartDto.CartHeader.CouponCode = "";
                ResponseDto? response = await _cartService.ApplyCouponAsync(cartDto);
                if (response != null & response.IsSuccess)
                {
                    TempData["success"] = "Cart updated successfully";
                    return RedirectToAction(nameof(CartIndex));
                }
                return View();
            }
            catch (Exception ex)
            {

                TempData["error"] = ex;
            }
            return View();
        }
        private async Task<CartDto> LoadCartDtoBasedOnLoggedInUser()
        {
            try
            {
                var userId = User.Claims.Where(u => u.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
                ResponseDto? response = await _cartService.GetCartByUserIdAsnyc(userId);
                if (response!=null & response.IsSuccess)
                {
                    CartDto cartDto = JsonConvert.DeserializeObject<CartDto>(Convert.ToString(response.Result));
                    return cartDto;
                }
                return new CartDto();
            }
            catch (Exception ex)
            {

                TempData["error"] = ex;
            }
            return new CartDto();
        }
        [HttpPost]
        public async Task<IActionResult> IncreaseCartItem(int cartDetailsId)
        {
            try
            {
                var userId = User.Claims.Where(u => u.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
                ResponseDto? response = await _cartService.IncreaseCartItem(cartDetailsId);
                if (response != null & response.IsSuccess)
                {
                    TempData["success"] = response;
                    return RedirectToAction(nameof(CartIndex));
                }
                else
                {
                    TempData["error"] = response;

                }
                return new JsonResult(response);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
            }
            return View();

        }
        [HttpPost]
        public async Task<IActionResult> DecreaseCartItem(int cartDetailsId)
        {
            try
            {
                var userId = User.Claims.Where(u => u.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
                ResponseDto? response = await _cartService.DecreaseCartItem(cartDetailsId);
                if (response != null & response.IsSuccess)
                {
                    TempData["success"] = "Cart updated successfully";
                    return RedirectToAction(nameof(CartIndex));
                }
                return new JsonResult(response);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
            }
            return View();

        }
        [HttpPost]
        public async Task<IActionResult> UpdateCartItem(int cartId)
        {
            try
            {
                var userId = User.Claims.Where(u => u.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
                ResponseDto? response = await _cartService.UpdateCartItem(cartId);
                if (response != null & response.IsSuccess)
                {
                    TempData["success"] = "Cart updated successfully";
                    return RedirectToAction(nameof(CartIndex));
                }
                return new JsonResult(response);
            }
            catch (Exception ex)
            {

                TempData["error"] = ex;
            }
            return View();
        }
    }
}
