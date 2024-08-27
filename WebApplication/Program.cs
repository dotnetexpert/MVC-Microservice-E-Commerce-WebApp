using Microsoft.AspNetCore.Authentication.Cookies;
using PayPalCheckoutSdk.Core;
using WebApplicationUI.Service;
using WebApplicationUI.Service.IService;
using WebApplicationUI.SignalR;
using WebApplicationUI.Utility;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IProductService, ProductService>();
//builder.Services.AddHttpClient<ICouponService, CouponService>();
builder.Services.AddHttpClient<ICartService, CartService>();
builder.Services.AddHttpClient<IWishlistService, WishlistService>();
builder.Services.AddHttpClient<IAuthService, AuthService>();
builder.Services.AddHttpClient<IOrderService, OrderService>();
SD.CouponAPIBase = builder.Configuration["ServiceUrls:CouponAPI"];
SD.OrderAPIBase= builder.Configuration["ServiceUrls:OrderAPI"];
SD.ShoppingCartAPIBase = builder.Configuration["ServiceUrls:ShoppingCartAPI"];
SD.AuthAPIBase = builder.Configuration["ServiceUrls:AuthAPI"];
SD.ProductAPIBase= builder.Configuration["ServiceUrls:ProductAPI"];
builder.Services.AddScoped<ITokenProvider, TokenProvider>();
builder.Services.AddScoped<IBaseService, BaseService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddHttpContextAccessor();
//builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddSignalR();
builder.Services.AddSession();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromHours(10);
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
    });

builder.Services.AddSingleton<PayPalHttpClient>(serviceProvider =>
{
    // Retrieve PayPal client credentials from configuration or any other source
    var clientId = "AcI7E7LiZl30Xch4nKpGCkuWRNbmS4BWAjwkqhvKLhPP3FjWAsjgrd7YAHaDdxb_f4sxTGAl_Y3t66yp";
    var clientSecret = "ECSet-AFfyroZDadhaoGSrWBPw6bB97jxQO2jVpvUkRkjRZCEm8jzh5cs_6m91PxLw9y2uGe6Z4cTb5g";

    // Create PayPal environment
    var environment = new SandboxEnvironment(clientId, clientSecret);

    // Create PayPal HTTP client
    var client = new PayPalHttpClient(environment);

    return client;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<CartHub>("/cartHub");
app.Run();
