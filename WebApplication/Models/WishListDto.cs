namespace WebApplicationUI.Models
{
    public class WishListDto
    {
        public CartHeaderDto CartHeader { get; set; }
        public IEnumerable<WishListIteamDto>? wishlistItems { get; set; }
    }
}
