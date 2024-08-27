using Microsoft.AspNetCore.Mvc;
using WebApplicationUI.Service.IService;
using WebApplicationUI.Models;

using Newtonsoft.Json;
using System.Collections.Generic;
using WebApplicationUI.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Text.RegularExpressions;
namespace WebApplicationUI.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICartService _cartService;
        public ProductController(IProductService productService, ICartService cartService)
        {
            _productService=productService;
            _cartService=cartService;
        }


        public async Task<IActionResult> ProductIndex()
        {
         
            try
            {
                List<ProductDto>? list = new();

                ResponseDto? response = await _productService.GetAllProductsAsync();

                if (response != null && response.IsSuccess)
                {
                    list= JsonConvert.DeserializeObject<List<ProductDto>>(Convert.ToString(response.Result));
                }
                else
                {
                    TempData["error"] = response?.Message;
                }
                return View(list);
            }
            catch (Exception ex)
            {

                TempData["error"] =ex;
            }
            return View();
        }

        public async Task<IActionResult> ProductCreate()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProductCreate(ProductDto model)
        {
            model.Description = StripHTML(model.Description);
            if (ModelState.IsValid)
            {
                try
                {
                    ResponseDto? response = await _productService.CreateProductsAsync(model);

                    if (response != null && response.IsSuccess)
                    {
                        TempData["success"] = "Product created successfully";
                        return RedirectToAction(nameof(ProductIndex));
                    }
                    else
                    {
                        TempData["error"] = response?.Message;
                    }
                }
                catch (Exception ex)
                {

                    TempData["error"] =ex;
                }               
            }
            return View(model);
        }
        public async Task<IActionResult> ProductDelete(int productId)
        {
            try
            {
                ResponseDto? response = await _productService.GetProductByIdAsync(productId);
                if (response != null && response.IsSuccess)
                {
                    ProductDto? model = JsonConvert.DeserializeObject<ProductDto>(Convert.ToString(response.Result));
                    return View(model);
                }
                else
                {
                    TempData["error"] = response?.Message;
                }
                return NotFound();
            }
            catch (Exception ex)
            {

                TempData["error"] =ex;
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> ProductDelete(ProductDto productDto)
        {
            try
            {
                ResponseDto? response = await _productService.DeleteProductsAsync(productDto.ProductId);

                if (response != null && response.IsSuccess)
                {
                    TempData["success"] = "Product deleted successfully";
                    return RedirectToAction(nameof(ProductIndex));
                }
                else
                {
                    TempData["error"] = response?.Message;
                }
                return View(productDto);
            }
            catch (Exception ex)
            {

                TempData["error"] =ex;
            }
            return NotFound();
        }

        public async Task<IActionResult> ProductEdit(int productId)
        {
            try
            {
                ResponseDto? response = await _productService.GetProductByIdAsync(productId);

                if (response != null && response.IsSuccess)
                {
                    ProductDto? model = JsonConvert.DeserializeObject<ProductDto>(Convert.ToString(response.Result));
                    return View(model);
                }
                else
                {
                    TempData["error"] = response?.Message;
                }
                return NotFound();
            }
            catch (Exception ex)
            {

                TempData["error"] =ex;
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> ProductEdit(ProductDto productDto)
        {
            productDto.Description = StripHTML(productDto.Description);
            try
            {
                if (ModelState.IsValid)
                {
                    ResponseDto? response = await _productService.UpdateProductsAsync(productDto);

                    if (response != null && response.IsSuccess)
                    {
                        TempData["success"] = "Product updated successfully";
                        return RedirectToAction(nameof(ProductIndex));
                    }
                    else
                    {
                        TempData["error"] = response?.Message;
                    }
                }
                return View(productDto);
            }
            catch (Exception ex)
            {

                TempData["error"] =ex;
            }
            return View(productDto);
        
        }
        private async Task<CartDto> LoadCartDtoBasedOnLoggedInUser()
        {
            try
            {
                var userId = User.Claims.Where(u => u.Type == JwtRegisteredClaimNames.Sub)?.FirstOrDefault()?.Value;
                ResponseDto? response = await _cartService.GetCartByUserIdAsnyc(userId);
                if (response!=null & response.IsSuccess)
                {
                    CartDto cartDto = JsonConvert.DeserializeObject<CartDto>(Convert.ToString(response.Result));
                    return cartDto;
                }
                
            }
            catch (Exception ex)
            {
                TempData["error"] =ex;
            }
            return new CartDto();
        }
        private string StripHTML(string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }
    }
}
