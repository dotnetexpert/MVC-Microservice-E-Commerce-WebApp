using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using WebApplicationUI.Models;
using WebApplicationUI.Service;
using WebApplicationUI.Service.IService;

public class WishlistController : Controller
{
    private readonly IWishlistService _wishlistService;
    private readonly ICartService _cartService;
    private readonly IProductService _productService;
    private readonly NavigationManager Navigation;


    public WishlistController(IWishlistService wishlistService, ICartService cartService)
    {
        _wishlistService = wishlistService;
        _cartService=cartService;
    }

    [Authorize]
    public async Task<IActionResult> WishListIndex()
    {
        try
        {
            var data = await LoadWishlistDtoBasedOnLoggedInUser();
            int WishCount = data.WishListItems != null ? data.WishListItems.Count() : 0;
            HttpContext.Session.SetInt32("WishCount", WishCount);
            return View(data);
        }
        catch (Exception ex)
        {
            TempData["error"] =ex;
        }
        return View();
       
    }
    [HttpPost]
    [ActionName("CheckProductInWishlist")]
    public async Task<IActionResult> CheckProductInWishlist(int productId)
    {
        try
        {
            if (productId == 0)
            {
                return BadRequest();
            }
            var userId = User.Claims.Where(u => u.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
            ResponseDto response = await _wishlistService.CheckProductInWishlistAsync(productId, userId);

            if (response != null && response.IsSuccess)
            {
                return Json(new { result = true });
            }
            return Json(new { result = false });

        }
        catch (Exception ex)
        {
            TempData["error"] =ex;
        }
        return Json(new { result = false });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddToWishlist(ProductDto productDto)
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

            WishListIteamDto WishListItems = new WishListIteamDto()
            {
                Count = productDto.Count,
                ProductId = productDto.ProductId,
            };
            List<WishListIteamDto> wishListIteamDto = new() { WishListItems };
            cartDto.WishListItems = wishListIteamDto;

            ResponseDto? response = await _wishlistService.AddToWishList(cartDto);

            if (response != null && response.IsSuccess)
            {
                TempData["success"] = response.Message;
                return RedirectToAction("/WishListIndex");

            }
            else
            {
                TempData["error"] = response?.Message;
            }

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            TempData["error"] =ex;
        }
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ActionName("ProductWishList")]
    public async Task<IActionResult> WishListIndex(ProductDto productDto)
    {
        try
        {
            CartDto cartDto = new CartDto()
            {
                CartHeader = new CartHeaderDto
                {
                    UserId = User.Claims.Where(u => u.Type == JwtClaimTypes.Subject)?.FirstOrDefault()?.Value
                },

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
                TempData["success"] = "Item has been added to the Shopping Cart";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["error"] = response?.Message;
            }
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            TempData["error"] =ex;
        }
        return RedirectToAction("Index", "Home");

    }
    public async Task<IActionResult> RemoveFromWishlist(int wishlistItemId)
    {
        try
        {
            var responseDto = await _wishlistService.RemoveFromWishListAsync(wishlistItemId);

            if (!responseDto.IsSuccess)
            {
                TempData["error"] = responseDto.Message;
            }
            else
            {
                TempData["success"] = "Product removed from wishlist successfully";
            }

            return RedirectToAction(nameof(WishListIndex));
        }
        catch (Exception ex)
        {
            TempData["error"] =ex;
        }
        return RedirectToAction(nameof(WishListIndex));
    }
    private async Task<CartDto> LoadWishlistDtoBasedOnLoggedInUser()
    {
        try
        {
            var userId = User.Claims.Where(u => u.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
            ResponseDto? response = await _wishlistService.GetWishListByUserIdAsync(userId);
            if (response!=null & response.IsSuccess)
            {
                CartDto wishList = JsonConvert.DeserializeObject<CartDto>(Convert.ToString(response.Result));
                return wishList;
            }
           
        }
        catch (Exception ex)
        {
            TempData["error"] =ex;
        }
        return new CartDto();
    }


}
