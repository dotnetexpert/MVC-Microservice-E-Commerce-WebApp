using AutoMapper;
using MessageBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using OrderAPI.Data;
using OrderAPI.Models;
using OrderAPI.Models.Dto;
using OrderAPI.Service.IService;
using OrderAPI.Utility;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using Stripe;
using Stripe.Checkout;
using System.Net;

namespace OrderAPI.Controllers
{
    [Route("api/order")]
    [ApiController]
    public class OrderAPIController : ControllerBase
    {
        protected ResponseDto _response;
        private IMapper _mapper;
        private readonly ApplicationDbContext _db;
        private IProductService _productService;
        private readonly IMessageBus _messageBus;
        private readonly IConfiguration _configuration;
        private readonly IMailService _mailService;
        public OrderAPIController(ApplicationDbContext db,
            IProductService productService, IMapper mapper, IConfiguration configuration
            , IMessageBus messageBus, IMailService mailService)
        {
            _db = db;
            _messageBus = messageBus;
            this._response = new ResponseDto();
            _productService = productService;
            _mapper = mapper;
            _configuration = configuration;
            _mailService= mailService;

        }

        [Authorize]
        [HttpGet("GetOrders")]
        public ResponseDto? Get(string? userId = "")
        {
            try
            {
                IEnumerable<OrderHeader> objList;
                if (User.IsInRole(SD.RoleAdmin))
                {
                    objList = _db.OrderHeaders.Include(u => u.OrderDetails).OrderByDescending(u => u.OrderHeaderId).ToList();
                }
                else
                {
                    objList = _db.OrderHeaders.Include(u => u.OrderDetails).Where(u => u.UserId==userId).OrderByDescending(u => u.OrderHeaderId).ToList();
                }
                _response.Result = _mapper.Map<IEnumerable<OrderHeaderDto>>(objList);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }
        [Authorize]
        [HttpGet("GetMostSell")]
        public async Task<ResponseDto?> GetMostSellAsync(string? userId = "")
        {
            try
            {
                var mostSoldProducts = new List<ProductDto>();

                IQueryable<OrderHeader> query;

                if (User.IsInRole(SD.RoleAdmin))
                {
                    query = _db.OrderHeaders.Include(u => u.OrderDetails).OrderByDescending(u => u.OrderHeaderId);
                }
                else
                {
                    query = _db.OrderHeaders.Include(u => u.OrderDetails).Where(u => u.UserId == userId).OrderByDescending(u => u.OrderHeaderId);
                }

                var productSales = query
                    .SelectMany(o => o.OrderDetails)
                    .GroupBy(od => od.ProductId)
                    .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(od => od.Count) })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(6);

                var products = await _productService.GetProducts();
                foreach (var productSale in productSales)
                {
                    var product = products.FirstOrDefault(p => p.ProductId == productSale.ProductId);
                    if (product != null)
                    {
                        mostSoldProducts.Add(product);
                    }
                }

                if (mostSoldProducts.Any())
                {
                    _response.Result = _mapper.Map<IEnumerable<ProductDto>>(mostSoldProducts);
                }
                else
                {
                    _response.Message = "No products sold yet.";
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }
        [Authorize]
        [HttpGet("GetOrder/{id:int}")]
        public ResponseDto? Get(int id)
        {
            try
            {
                OrderHeader orderHeader = _db.OrderHeaders.Include(u => u.OrderDetails).First(u => u.OrderHeaderId == id);
                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }



        [Authorize]
        [HttpPost("CreateOrder")]
        public async Task<ResponseDto> CreateOrder([FromBody] CartDto cartDto)
        {
            try
            {
                OrderHeaderDto orderHeaderDto = _mapper.Map<OrderHeaderDto>(cartDto.CartHeader);
                orderHeaderDto.OrderTime = DateTime.Now;
                orderHeaderDto.Status = SD.Status_Pending;
                orderHeaderDto.OrderDetails = _mapper.Map<IEnumerable<OrderDetailsDto>>(cartDto.CartDetails);
                orderHeaderDto.OrderTotal = Math.Round(orderHeaderDto.OrderTotal, 2);
                OrderHeader orderCreated = _db.OrderHeaders.Add(_mapper.Map<OrderHeader>(orderHeaderDto)).Entity;
                await _db.SaveChangesAsync();

                orderHeaderDto.OrderHeaderId = orderCreated.OrderHeaderId;
                _response.Result = orderHeaderDto;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message=ex.Message;
            }
            return _response;
        }
        [Authorize]
        [HttpPost("CreatePayPalPayment")]
        public async Task<ResponseDto> CreatePayPalPayment([FromBody] PayPalRequestDto payPalRequestDto)
        {
            try
            {
                var clientId = _configuration["PayPal:ClientId"];
                var secret = _configuration["PayPal:Secret"];
                var environment = new SandboxEnvironment(clientId, secret);
                var client = new PayPalHttpClient(environment);
                var request = new OrdersCreateRequest();
                request.Prefer("return=representation");
                request.RequestBody(BuildRequestBody(payPalRequestDto));
                var response = await client.Execute(request);
                var statusCode = response.StatusCode;
                if (statusCode == HttpStatusCode.Created)
                {
                    var responseBody = response.Result<PayPalCheckoutSdk.Orders.Order>();
                    var approvalUrl = responseBody.Links.FirstOrDefault(link => link.Rel == "approve")?.Href;
                    var responseDto = new PayPalRequestDto { ApprovedUrl = approvalUrl };
                    _response.Result = responseDto;
                    return _response;
                }
                else
                {
                    var responseDto = new ResponseDto { Message = "Failed to create PayPal payment" };
                    return responseDto;
                }
            }
            catch (Exception ex)
            {
                var responseDto = new ResponseDto { Message = $"An error occurred: {ex.Message}" };
                return responseDto;
            }
        }
        private OrderRequest BuildRequestBody(PayPalRequestDto payPalRequestDto)
        {
            var purchaseUnits = new List<PurchaseUnitRequest>();
            int referenceIdCounter = 0;
            foreach (var item in payPalRequestDto.OrderHeader.OrderDetails)
            {
                referenceIdCounter++;
                string truncatedDescription = item.Product.Description.Length > 127
          ? item.Product.Description.Substring(0, 127)
          : item.Product.Description;
                var purchaseUnit = new PurchaseUnitRequest()
                {
                    AmountWithBreakdown = new AmountWithBreakdown()
                    {
                        Value = item.Price.ToString(),
                        CurrencyCode = "usd",
                        AmountBreakdown = new AmountBreakdown()
                    },
                    Description = truncatedDescription,
                    CustomId=item.ProductId.ToString(),
                    ReferenceId = referenceIdCounter.ToString()
                };
                purchaseUnits.Add(purchaseUnit);
            }
            var request = new OrderRequest()
            {
                CheckoutPaymentIntent = "CAPTURE",
                PurchaseUnits = purchaseUnits,
                ApplicationContext = new ApplicationContext()
                {
                    ReturnUrl = payPalRequestDto.ApprovedUrl,
                    CancelUrl = payPalRequestDto.CancelUrl
                }
            };
            return request;
        }
        [Authorize]
        [HttpPost("CreateStripeSession")]
        public async Task<ResponseDto> CreateStripeSession([FromBody] StripeRequestDto stripeRequestDto)
        {
            try
            {
                var options = new SessionCreateOptions
                {
                    SuccessUrl = stripeRequestDto.ApprovedUrl,
                    CancelUrl = stripeRequestDto.CancelUrl,
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };
                var DiscountsObj = new List<SessionDiscountOptions>()
                {
                    new SessionDiscountOptions
                    {
                        Coupon=stripeRequestDto.OrderHeader.CouponCode
                    }
                };
                foreach (var item in stripeRequestDto.OrderHeader.OrderDetails)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100), // $20.99 -> 2099
                            Currency = "usd",/*GetCurrencyFromIP(),*/
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Name,
                                Description = item.Product.Description,
                                Images = new List<string> { item.Product.ImageUrl }
                            }
                        },
                        Quantity = item.Count,
                    };
                    options.LineItems.Add(sessionLineItem);
                }
                if (stripeRequestDto.OrderHeader.Discount > 0)
                {
                    options.Discounts = DiscountsObj;
                }
                var service = new SessionService();
                Session session = service.Create(options);
                stripeRequestDto.StripeSessionUrl = session.Url;
                OrderHeader orderHeader = _db.OrderHeaders.First(u => u.OrderHeaderId == stripeRequestDto.OrderHeader.OrderHeaderId);
                orderHeader.StripeSessionId = session.Id;
                _db.SaveChanges();
                _response.Result = stripeRequestDto;

            }
            catch (Exception ex)
            {
                _response.Message= ex.Message;
                _response.IsSuccess = false;
            }
            return _response;
        }    
        [Authorize]
        [HttpPost("ValidatePayPal")]
        public async Task<IActionResult> ValidatePayPal([FromBody] int orderHeaderId)
        {
            var response = new ResponseDto(); 
            try
            {                
                OrderHeader orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(u => u.OrderHeaderId == orderHeaderId);
                if (orderHeader == null)
                {
                    return NotFound(); 
                }               
                if (orderHeader.Status == "Pending")
                {                    
                    orderHeader.Status = SD.Status_Approved;                  
                    orderHeader.OrderDetails = await _db.OrderDetails.Where(od => od.OrderHeaderId == orderHeaderId).ToListAsync();               
                    await _db.SaveChangesAsync();      
                    OrderHeaderDto orderHeaderDto = _mapper.Map<OrderHeaderDto>(orderHeader);
                    response.Result = orderHeaderDto;
                    response.IsSuccess = true;
                }
                else
                {                    
                    response.IsSuccess = false;
                    response.Message = "Order status is not 'succeeded'.";
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return Ok(response); 
        }
        [Authorize]
        [HttpPost("ValidateStripeSession")]
        public async Task<ResponseDto> ValidateStripeSession([FromBody] int orderHeaderId)
        {
            try
            {
                OrderHeader orderHeader = _db.OrderHeaders.First(u => u.OrderHeaderId == orderHeaderId);
                var service = new SessionService();
                Session session = service.Get(orderHeader.StripeSessionId);
                var paymentIntentService = new PaymentIntentService();
                PaymentIntent paymentIntent = paymentIntentService.Get(session.PaymentIntentId);
                if (paymentIntent.Status == "succeeded")
                {
                    orderHeader.PaymentIntentId = paymentIntent.Id;
                    orderHeader.Status = SD.Status_Approved;
                    // Fetch order details
                    orderHeader.OrderDetails = _db.OrderDetails.Where(od => od.OrderHeaderId == orderHeaderId).ToList();
                    _db.SaveChanges();
                    RewardsDto rewardsDto = new()
                    {
                        OrderId = orderHeader.OrderHeaderId,
                        RewardsActivity = Convert.ToInt32(orderHeader.OrderTotal),
                        UserId = orderHeader.UserId
                    };
                    string topicName = _configuration.GetValue<string>("TopicAndQueueNames:OrderCreatedTopic");
                    //await _messageBus.PublishMessage(rewardsDto, topicName);
                    // Map orderHeader to OrderHeaderDto
                    OrderHeaderDto orderHeaderDto = _mapper.Map<OrderHeaderDto>(orderHeader);
                    _response.Result = orderHeaderDto;
                }
            }
            catch (Exception ex)
            {
                _response.Message = ex.Message;
                _response.IsSuccess = false;
            }
            return _response;
        }


        [Authorize]
        [HttpPost("UpdateOrderStatus/{orderId:int}")]
        public async Task<ResponseDto> UpdateOrderStatus(int orderId, [FromBody] string newStatus)
        {
            try
            {
                OrderHeader orderHeader = _db.OrderHeaders.First(u => u.OrderHeaderId == orderId);
                if (orderHeader != null)
                {
                    if (newStatus == SD.Status_Cancelled)
                    {
                        var options = new RefundCreateOptions
                        {
                            Reason = RefundReasons.RequestedByCustomer,
                            PaymentIntent = orderHeader.PaymentIntentId
                        };

                        var service = new RefundService();
                        Stripe.Refund refund = service.Create(options);
                    }
                    var data = await _productService.GetProducts();
                    foreach (var product in data)
                    {
                        if (product != null)
                        {

                        }
                    }
                    orderHeader.Status = newStatus;
                    _db.SaveChanges();

                    if (newStatus == SD.Status_Completed)
                    {
                        MailRequest request = new MailRequest
                        {
                            Subject = "Order Status Updated",
                            Body = $"Dear {orderHeader.Name},\n\nWe are writing to inform you that the status of your order with ID {orderId} has been updated.\n\nOrder ID: {orderId}\nNew Status: {newStatus}\n\nFor any questions or concerns regarding your order, please don't hesitate to contact us at 8894568424.\n\nThank you for choosing Micro Service!\n\nBest regards,\nAbhishek Pambra\nJr.Software Engineer\nMicro Service",
                            ToEmail = orderHeader.Email
                        };

                        await SendMail(request);
                    }   
                }
            }

            catch (Exception ex)
            {
                _response.IsSuccess = false;
            }
            return _response;
        }
        [HttpGet]
        public string GetCurrencyFromIP()
        {
            string currencyCode = "usd";
            IPHostEntry heserver = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = heserver.AddressList
                .FirstOrDefault(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();
            if (IsPrivateIpAddress(ipAddress))
            {
                Console.WriteLine("Private IP address detected. Defaulting to USD.");
                return currencyCode;
            }
            try
            {
                string url = $"http://ip-api.com/json/{ipAddress}";
                using (WebClient client = new WebClient())
                {
                    string json = client.DownloadString(url);
                    JObject data = JObject.Parse(json);
                    string countryCode = (string)data["countryCode"];
                    switch (countryCode)
                    {
                        case "US":
                            currencyCode = "usd";
                            break;
                        case "GB":
                            currencyCode = "gbp";
                            break;
                        case "IN":
                            currencyCode = "inr";
                            break;
                        default:
                            currencyCode = "usd";
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return currencyCode;
        }
        private bool IsPrivateIpAddress(string ipAddress)
        {
            if (ipAddress == null) return true;
            string[] parts = ipAddress.Split('.');
            int firstByte = int.Parse(parts[0]);
            return firstByte switch
            {
                10 => true,
                172 => int.Parse(parts[1]) >= 16 && int.Parse(parts[1]) <= 31,
                192 => int.Parse(parts[1]) == 168,
                _ => false
            };
        }
        [HttpPost("send")]
        public async Task<IActionResult> SendMail([FromForm] MailRequest request)
        {
            try
            {
                await _mailService.SendEmailAsync(request);
                return Ok();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        //public async Task<string> GetCountryCodeFromCurrentLocation()
        //{
        //    try
        //    {
        //        Geolocator geolocator = new Geolocator();
        //        Geoposition geoposition = await geolocator.GetGeopositionAsync();
        //        // Retrieve the latitude and longitude
        //        double latitude = geoposition.Coordinate.Point.Position.Latitude;
        //        double longitude = geoposition.Coordinate.Point.Position.Longitude;
        //        // Use a geocoding service to get the country code from the latitude and longitude
        //        string countryCode = await GetCountryCodeFromCoordinates(latitude, longitude);
        //        return countryCode;
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle errors
        //        Console.WriteLine("Error: " + ex.Message);
        //        return null;
        //    }
        //}
        //private async Task<string> GetCountryCodeFromCoordinates(double latitude, double longitude)
        //{
        //    // Implement logic to convert latitude and longitude to a country code using a geocoding service
        //    // This could involve using a reverse geocoding API provided by services like Google Maps or Bing Maps
        //    // Here's a simplified example:
        //    // string countryCode = await SomeGeocodingService.GetCountryCode(latitude, longitude);
        //    // return countryCode;
        //    // For demonstration purposes, returning a hardcoded value
        //    return "US";
        //}
    }
}

