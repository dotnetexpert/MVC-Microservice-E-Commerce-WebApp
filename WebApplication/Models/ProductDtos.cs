﻿using System.ComponentModel.DataAnnotations;
using WebApplicationUI.Utility;

namespace WebApplicationUI.Models
{
    public class ProductDtos
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageLocalPath { get; set; }
        [Range(1, 1000)]
        public int Count { get; set; } = 1;
        [MaxFileSize(1)]
        public IFormFile? Image { get; set; }
    }
}
