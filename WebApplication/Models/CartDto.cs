namespace WebApplicationUI.Models
{
    public class CartDto
    {
        public CartHeaderDto CartHeader { get; set; }
        public IEnumerable<CartDetailsDto>? CartDetails { get; set; }
        public WishListIteamDto wish { get; set; }
        public IEnumerable<WishListIteamDto>? WishListItems { get; set; }
    }
}
