using AutoMapper;
using Azure;
using MessageBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingCartAPI.Data;
using ShoppingCartAPI.Models;
using ShoppingCartAPI.Models.Dto;
using ShoppingCartAPI.Service.IService;

namespace ShoppingCartAPI.Controllers
{
    [Route("api/cart")]
    [ApiController]
    public class CartAPIController : ControllerBase
    {
        private ResponseDto _response;
        private IMapper _mapper;
        private readonly ApplicationDbContext _db;
        private IProductService _productService;
        private ICouponService _couponService;
        private IConfiguration _configuration;
        private readonly IMessageBus _messageBus;
        public CartAPIController(ApplicationDbContext db,
            IMapper mapper, IProductService productService, ICouponService couponService, IMessageBus messageBus, IConfiguration configuration)
        {
            _db = db;
            _messageBus = messageBus;
            _productService = productService;
            this._response = new ResponseDto();
            _mapper = mapper;
            _couponService = couponService;
            _configuration = configuration;
        }
        [HttpGet("GetCart/{userId}")]
        public async Task<ResponseDto> GetCart(string userId)
        {
            try
            {
                CartDto cart = new()
                {
                    CartHeader = _mapper.Map<CartHeaderDto>(_db.CartHeaders.First(u => u.UserId == userId))
                };
                cart.CartDetails = _mapper.Map<IEnumerable<CartDetailsDto>>(_db.CartDetails
                    .Where(u => u.CartHeaderId==cart.CartHeader.CartHeaderId));

                IEnumerable<ProductDto> productDtos = await _productService.GetProducts();

                foreach (var item in cart.CartDetails)
                {
                    item.Product = productDtos.FirstOrDefault(u => u.ProductId == item.ProductId);
                    cart.CartHeader.CartTotal += (item.Count * item.Product.Price);
                }

                //apply coupon if any
                if (!string.IsNullOrEmpty(cart.CartHeader.CouponCode))
                {
                    CouponDto coupon = await _couponService.GetCoupon(cart.CartHeader.CouponCode);
                    if (coupon!=null && cart.CartHeader.CartTotal > coupon.MinAmount)
                    {
                        cart.CartHeader.CartTotal -= coupon.DiscountAmount;
                        cart.CartHeader.Discount=coupon.DiscountAmount;
                    }
                }

                _response.Result=cart;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }


        [HttpPost("ApplyCoupon")]
        public async Task<object> ApplyCoupon([FromBody] CartDto cartDto)
        {
            try
            {
                var cartFromDb = await _db.CartHeaders.FirstAsync(u => u.UserId == cartDto.CartHeader.UserId);
                cartFromDb.CouponCode = cartDto.CartHeader.CouponCode;
                _db.CartHeaders.Update(cartFromDb);
                await _db.SaveChangesAsync();
                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.ToString();
            }
            return _response;
        }

        [HttpPost("EmailCartRequest")]
        public async Task<object> EmailCartRequest([FromBody] CartDto cartDto)
        {
            try
            {
                await _messageBus.PublishMessage(cartDto, _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue"));
                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.ToString();
            }
            return _response;
        }
        [HttpPost("CartUpsert")]
        public async Task<ResponseDto> CartUpsert(CartDto cartDto)
        {
            try
            {
                var cartHeaderFromDb = await _db.CartHeaders.FirstOrDefaultAsync(u => u.UserId == cartDto.CartHeader.UserId);
                if (cartHeaderFromDb == null)
                {
                    CartHeader cartHeader = _mapper.Map<CartHeader>(cartDto.CartHeader);
                    _db.CartHeaders.Add(cartHeader);
                    await _db.SaveChangesAsync();
                    cartDto.CartDetails.First().CartHeaderId = cartHeader.CartHeaderId;
                    _db.CartDetails.Add(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                    await _db.SaveChangesAsync();

                }
                else
                {
                    var cartDetailsFromDb = await _db.CartDetails.FirstOrDefaultAsync(
                        u => u.ProductId == cartDto.CartDetails.First().ProductId &&
                             u.CartHeaderId == cartHeaderFromDb.CartHeaderId);
                    if (cartDetailsFromDb == null)
                    {
                        cartDto.CartDetails.First().CartHeaderId = cartHeaderFromDb.CartHeaderId;
                        _db.CartDetails.Add(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                        await _db.SaveChangesAsync();
                        _response.Result ="Add to card successfully";
                    }
                    else
                    {
                        int upcomingCount = cartDto.CartDetails.First().Count;
                        int oldCount = cartDetailsFromDb.Count;

                        IEnumerable<ProductDto> productDtos = await _productService.GetProducts();
                        int availableCount = productDtos.FirstOrDefault(p => p.ProductId == cartDto.CartDetails.First().ProductId)?.Count ?? 0;
                        int newCount = upcomingCount + oldCount;
                        if (newCount > availableCount)
                        {
                            newCount = availableCount;
                            cartDetailsFromDb.Count = newCount;
                            _db.CartDetails.Update(cartDetailsFromDb);
                            await _db.SaveChangesAsync();
                            _response.Result = "Limit Exceeded";
                        }
                        else
                        {
                            cartDetailsFromDb.Count = newCount;                           
                            _db.CartDetails.Update(cartDetailsFromDb);
                            await _db.SaveChangesAsync();                           
                            _response.Result = "Add count successfully";
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                _response.Message = ex.Message.ToString();
                _response.IsSuccess = false;
            }
            return _response;
        }
        [HttpPost("IncreaseCartItem/{cartDetailsId}")]
        public async Task<ResponseDto> IncreaseCartItem(int cartDetailsId)
        {
            try
            {
                var cartDetail = await _db.CartDetails.FindAsync(cartDetailsId);
                if (cartDetail != null)
                {
                    IEnumerable<ProductDto> productDtos = await _productService.GetProducts();
                    if (productDtos != null)
                    {
                        // Check if the count of the cart item is less than or equal to the count of the corresponding product
                        var correspondingProduct = productDtos.FirstOrDefault(p => p.ProductId == cartDetail.ProductId);
                        if (correspondingProduct != null && cartDetail.Count < correspondingProduct.Count)
                        {
                            cartDetail.Count++;
                            await _db.SaveChangesAsync();
                            _response.IsSuccess = true;
                        }
                        else
                        {
                            _response.IsSuccess = false;
                            _response.Message = "Cannot increase count, reached maximum available count for the product";
                        }
                    }
                    else
                    {
                        _response.IsSuccess = false;
                        _response.Message = "Error fetching product information";
                    }
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.Message = "Cart item not found";
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }
        [HttpPost("DecreaseCartItem/{cartDetailsId}")]
        public async Task<ResponseDto> DecreaseCartItem(int cartDetailsId)
        {
            try
            {
                var cartDetail = await _db.CartDetails.FindAsync(cartDetailsId);
                if (cartDetail != null)
                {
                    if (cartDetail.Count > 1)
                    {
                        cartDetail.Count--;
                        await _db.SaveChangesAsync();
                        _response.IsSuccess = true;
                    }
                    else
                    {
                        _db.CartDetails.Remove(cartDetail);
                        await _db.SaveChangesAsync();
                        _response.IsSuccess = true;
                    }
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.Message = "Cart item not found";
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }
        [HttpPost("RemoveCart")]
        public async Task<ResponseDto> RemoveCart([FromBody] int cartDetailsId)
        {
            try
            {
                CartDetails cartDetails = _db.CartDetails
                   .First(u => u.CartDetailsId == cartDetailsId);

                int totalCountofCartItem = _db.CartDetails.Where(u => u.CartHeaderId == cartDetails.CartHeaderId).Count();
                _db.CartDetails.Remove(cartDetails);
                //if (totalCountofCartItem == 1)
                //{
                //    var cartHeaderToRemove = await _db.CartHeaders
                //       .FirstOrDefaultAsync(u => u.CartHeaderId == cartDetails.CartHeaderId);

                //    _db.CartHeaders.Remove(cartHeaderToRemove);
                //}
                await _db.SaveChangesAsync();

                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.Message = ex.Message.ToString();
                _response.IsSuccess = false;
            }
            return _response;
        }

        [HttpPost("AddToWishList")]
        public async Task<ResponseDto> AddToWishList(CartDto cartDto)
        {
            try
            {
                var cartHeaderFromDb = await _db.CartHeaders.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == cartDto.CartHeader.UserId);
                if (cartHeaderFromDb == null)
                {
                    //create header and details
                    CartHeader cartHeader = _mapper.Map<CartHeader>(cartDto.CartHeader);
                    _db.CartHeaders.Add(cartHeader);
                    await _db.SaveChangesAsync();
                    cartDto.WishListItems.First().CartHeaderId = cartHeader.CartHeaderId;
                    _db.WishListItems.Add(_mapper.Map<WishListItem>(cartDto.WishListItems.First()));
                    await _db.SaveChangesAsync();
                }
                else
                {
                    //if header is not null
                    //check if details has same product
                    var cartWishlistFromDb = await _db.WishListItems.AsNoTracking().FirstOrDefaultAsync(
                        u => u.ProductId == cartDto.WishListItems.First().ProductId &&
                        u.CartHeaderId == cartHeaderFromDb.CartHeaderId);
                    if (cartWishlistFromDb == null)
                    {
                        //create cartdetails
                        cartDto.WishListItems.First().CartHeaderId = cartHeaderFromDb.CartHeaderId;
                        _db.WishListItems.Add(_mapper.Map<WishListItem>(cartDto.WishListItems.First()));
                        await _db.SaveChangesAsync();
                        _response.Message ="Add in WishList";
                    }
                    else
                    {
                        _response.Message ="Already in WishList";
                    }
                }
                _response.Result = cartDto;
            }
            catch (Exception ex)
            {
                _response.Message= ex.Message.ToString();
                _response.IsSuccess= false;
            }
            return _response;
        }



        [HttpGet("GetWishlist/{userId}")]
        public async Task<ResponseDto> GetWishlist(string userId)
        {
            try
            {
                CartDto cart = new()
                {
                    CartHeader = _mapper.Map<CartHeaderDto>(_db.CartHeaders.First(u => u.UserId == userId))
                };
                cart.WishListItems = _mapper.Map<IEnumerable<WishListIteamDto>>(_db.WishListItems
                    .Where(u => u.CartHeaderId==cart.CartHeader.CartHeaderId));

                IEnumerable<ProductDto> productDtos = await _productService.GetProducts();

                foreach (var item in cart.WishListItems)
                {
                    item.Product = productDtos.FirstOrDefault(u => u.ProductId == item.ProductId);
                    cart.CartHeader.CartTotal += (item.Count * item.Product.Price);
                }

                //apply coupon if any
                if (!string.IsNullOrEmpty(cart.CartHeader.CouponCode))
                {
                    CouponDto coupon = await _couponService.GetCoupon(cart.CartHeader.CouponCode);
                    if (coupon!=null && cart.CartHeader.CartTotal > coupon.MinAmount)
                    {
                        cart.CartHeader.CartTotal -= coupon.DiscountAmount;
                        cart.CartHeader.Discount=coupon.DiscountAmount;
                    }
                }

                _response.Result=cart;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }



        [HttpPost("RemoveFromWishlist/{wishListItemId}")]
        public async Task<ResponseDto> RemoveFromWishlist(int wishListItemId)
        {
            try
            {
                var wishlistItem = await _db.WishListItems.FirstOrDefaultAsync(w => w.WishListItemId == wishListItemId);
                if (wishlistItem != null)
                {
                    _db.WishListItems.Remove(wishlistItem);
                    await _db.SaveChangesAsync();
                }
                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }
        [HttpPost("CheckProductInWishlistAsync/{ID}/{UserId}")]
        public async Task<ResponseDto> CheckProductInWishlistAsync(int Id, string UserId)
        {
            try
            {
                CartHeader cartHeader = await _db.CartHeaders.FirstOrDefaultAsync(u => u.UserId == UserId);

                if (cartHeader != null)
                {
                    var wishlistItems = await _db.WishListItems
                        .Where(w => w.CartHeaderId == cartHeader.CartHeaderId)
                        .ToListAsync();

                    var isProductInWishlist = wishlistItems.Any(w => w.ProductId == Id);
                    if (isProductInWishlist==true)
                    {
                        _response.IsSuccess = true;
                        _response.Result = true;
                       
                    }
                    else
                    {
                        _response.IsSuccess = false;
                        _response.Result = false;
                    }                  
                }               
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }



    }
}

