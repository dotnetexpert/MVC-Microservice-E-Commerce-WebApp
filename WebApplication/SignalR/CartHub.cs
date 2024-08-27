using Microsoft.AspNetCore.SignalR;
using WebApplicationUI.Models;

namespace WebApplicationUI.SignalR
{
    public class CartHub:Hub
    {
        public async Task UpdateCartData(CartDto cart)
        {
            await Clients.All.SendAsync("ReceiveCartDataUpdate", cart);
        }
        public async Task RemoveCartData(int id)
        {
            await Clients.All.SendAsync("RemoveCartData", id);
        }

    }
}
