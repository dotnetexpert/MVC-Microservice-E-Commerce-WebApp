using Microsoft.AspNetCore.Http.HttpResults;
using WebApplicationUI.Models;
using WebApplicationUI.Service.IService;
using WebApplicationUI.Utility;

namespace WebApplicationUI.Service
{
    public class WishlistService : IWishlistService
    {
        private readonly IBaseService _baseService;
        public WishlistService(IBaseService baseService)
        {
            _baseService = baseService;
        }

        public async Task<ResponseDto?> AddToWishList(CartDto cartDto)
        {
            
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = SD.ApiType.POST,
                Data = cartDto,
                Url = SD.ShoppingCartAPIBase + "/api/cart/AddToWishlist" 
            });
        }

        public async Task<ResponseDto?> CheckProductInWishlistAsync(int Id, string UserId)
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = SD.ApiType.POST,
                Data = new { Id, UserId },
                Url = SD.ShoppingCartAPIBase + $"/api/cart/CheckProductInWishlistAsync/{Id}/{UserId}"
            });
        }

        public async Task<ResponseDto?> GetWishListByUserIdAsync(string userId)
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = SD.ApiType.GET,
                Url = SD.ShoppingCartAPIBase + "/api/cart/GetWishlist/"+ userId
            });
        }

        public async Task<ResponseDto?> RemoveFromWishListAsync(int wishListItemId)
        {
            return await _baseService.SendAsync(new RequestDto()
            {
                ApiType = SD.ApiType.POST,
                Data = wishListItemId,
                Url = SD.ShoppingCartAPIBase + "/api/cart/RemoveFromWishlist/"+wishListItemId
            });
        }
    }
}
