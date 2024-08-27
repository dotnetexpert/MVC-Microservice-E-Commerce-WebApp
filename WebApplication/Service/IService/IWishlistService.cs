using WebApplicationUI.Models;

namespace WebApplicationUI.Service.IService
{
    public interface IWishlistService
    {
        Task<ResponseDto?> GetWishListByUserIdAsync(string userId);
        Task<ResponseDto?> AddToWishList(CartDto cartDto);
        Task<ResponseDto?> RemoveFromWishListAsync(int wishListItemId);
        Task<ResponseDto?> CheckProductInWishlistAsync(int Id ,string userId);
    }
}
