using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using WebApplicationUI.Models;
using WebApplicationUI.Service;
using WebApplicationUI.Service.IService;

namespace WebApplicationUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICartService _cartService;
        private readonly IWishlistService _wishlistService;
        private readonly IOrderService _orderService;
        ViewModel ViewModel { get; set; }
        public int? cartCount { get; set; }
        public HomeController(IProductService productService, ICartService cartService, IWishlistService wishlistService, IOrderService orderService)
        {
            _productService = productService;
            _cartService = cartService;
            _wishlistService=wishlistService;
            _orderService=orderService;
        }


        public async Task<IActionResult> Index()
        {
            try
            {
                var data = await LoadCartDtoBasedOnLoggedInUser();
                int cartCount = data.CartDetails != null ? data.CartDetails.Count() : 0;
                HttpContext.Session.SetInt32("CartCount", cartCount);

                var Wishlist = await LoadWishDtoBasedOnLoggedInUser();
                int WishCount = Wishlist.wishlistItems != null ? Wishlist.wishlistItems.Count() : 0;
                HttpContext.Session.SetInt32("WishCount", WishCount);
                ViewModel mymodel = new ViewModel();
                ResponseDto? response = await _productService.GetAllProductsAsync();
                if (response != null && response.IsSuccess)
                {
                    mymodel.parent = JsonConvert.DeserializeObject<List<ProductDto>>(Convert.ToString(response.Result));
                }
                else
                {
                    TempData["error"] = response?.Message;
                }
               
                var userId = User.Claims.Where(u => u.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
                ResponseDto? response1 = await _orderService.GetMostSell(userId);
                if (response1 != null && response1.IsSuccess)
                {
                    mymodel.child = JsonConvert.DeserializeObject<List<ProductDto>>(Convert.ToString(response1.Result));
                    //return View(ViewModel);
                }
                return View(mymodel);
            }
            catch (Exception ex)
            {

                TempData["error"] = ex;
            }
            return View();

        }
        [Authorize]
        public async Task<IActionResult> GetMostSell()
        {
            IEnumerable<ProductDtos> list;
            var userId = User.Claims.Where(u => u.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
            ResponseDto response = await _orderService.GetMostSell(userId);
            if (response != null && response.IsSuccess)
            {
                list = JsonConvert.DeserializeObject<List<ProductDtos>>(Convert.ToString(response.Result));
                return View(list); 
            }
            return NoContent();
        }

        [Authorize]
        public async Task<IActionResult> ProductDetails(int productId)
        {
            try
            {
                ProductDto? model = new();

                ResponseDto? response = await _productService.GetProductByIdAsync(productId);

                if (response != null && response.IsSuccess)
                {
                    model = JsonConvert.DeserializeObject<ProductDto>(Convert.ToString(response.Result));
                }
                else
                {
                    TempData["error"] = response?.Message;
                }

                return View(model);
            }
            catch (Exception ex)
            {

                TempData["error"] = ex;
            }
            return View();
        }

        [Authorize]
        [HttpPost]
        [ActionName("ProductDetails")]
        public async Task<IActionResult> ProductDetails(ProductDto productDto)
        {
            try
            {
                CartDto cartDto = new CartDto()
                {
                    CartHeader = new CartHeaderDto
                    {
                        UserId = User.Claims.Where(u => u.Type == JwtClaimTypes.Subject)?.FirstOrDefault()?.Value
                    }
                };

                CartDetailsDto cartDetails = new CartDetailsDto()
                {
                    Count = productDto.Count,
                    ProductId = productDto.ProductId,
                };

                List<CartDetailsDto> cartDetailsDtos = new() { cartDetails };
                cartDto.CartDetails = cartDetailsDtos;

                ResponseDto? response = await _cartService.UpsertCartAsync(cartDto);

                if (response != null && response.IsSuccess)
                {
                    TempData["success"] = response?.Result;
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["error"] = response?.Message;
                }

                return View(productDto);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
            }
            return View();

        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
                
            }
            catch (Exception ex)
            {

                TempData["error"] = ex;
            }
            return new CartDto();

        }
        private async Task<WishListDto> LoadWishDtoBasedOnLoggedInUser()
        {
            try
            {
                var userId = User.Claims.Where(u => u.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
                ResponseDto? response = await _wishlistService.GetWishListByUserIdAsync(userId);
                if (response!=null & response.IsSuccess)
                {
                    WishListDto wishList = JsonConvert.DeserializeObject<WishListDto>(Convert.ToString(response.Result));
                    return wishList;
                }               
            }
            catch (Exception ex)
            {

                TempData["error"] = ex;
            }
            return new WishListDto();

        }
    }
}
