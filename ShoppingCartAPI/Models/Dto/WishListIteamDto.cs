﻿using System.ComponentModel.DataAnnotations.Schema;

namespace ShoppingCartAPI.Models.Dto
{
    public class WishListIteamDto
    {
        public int WishListItemId { get; set; }
        public int CartHeaderId { get; set; }       
        public CartHeader? CartHeader { get; set; }
        public int ProductId { get; set; }     
        public ProductDto? Product { get; set; }
        public int Count { get; set; }
    }
}
