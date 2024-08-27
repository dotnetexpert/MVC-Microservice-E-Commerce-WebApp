using Microsoft.EntityFrameworkCore;
using ShoppingCartAPI.Models;
using System;

namespace ShoppingCartAPI.Data
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
                
        }
        public DbSet<CartDetails> CartDetails { get; set; }
        public DbSet<CartHeader> CartHeaders { get; set; }
        public DbSet<WishListItem> WishListItems { get; set; }

    }
}
