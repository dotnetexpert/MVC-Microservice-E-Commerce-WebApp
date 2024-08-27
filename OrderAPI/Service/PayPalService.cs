using Microsoft.AspNetCore.Components;
using OrderAPI.Models;
using OrderAPI.Models.Dto;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;

namespace OrderAPI.Service
{
    public class PayPalService
    {
        private readonly IConfiguration _configuration;
        private readonly NavigationManager _navigationManager;

        public PayPalService(IConfiguration configuration, NavigationManager navigationManager)
        {
            _configuration = configuration;
            _navigationManager = navigationManager;
        }

        public async Task<string> CreatePaymentLink(PayPalPaymentRequest paymentRequest, CartDto cartDto )
        {
            try
            {
                var payPalSettings = _configuration.GetSection("PayPal").Get<PayPalSettings>();
                // Set up PayPal environment with your credentials
                var environment = new SandboxEnvironment(payPalSettings.Key, payPalSettings.Secret);
                var client = new PayPalHttpClient(environment);

                // Create the order and retrieve the approval URL
                var orderRequest = BuildOrderRequest(paymentRequest, cartDto);
                var response = await client.Execute(new OrdersCreateRequest().Prefer("return=representation").RequestBody(orderRequest));

                var order = response.Result<Order>();
                var approvalUrl = order.Links.Find(link => link.Rel.Equals("approve")).Href;

                return approvalUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                throw;
            }
        }

        private OrderRequest BuildOrderRequest(PayPalPaymentRequest paymentRequest, CartDto cartDto)
        {
            var domain = _navigationManager.BaseUri.TrimEnd('/');

            return new OrderRequest
            {
                CheckoutPaymentIntent = "CAPTURE",
                ApplicationContext = new ApplicationContext
                {
                    LandingPage = "BILLING",
                    UserAction = "CONTINUE",
                    ShippingPreference = "SET_PROVIDED_ADDRESS",
                    ReturnUrl = domain + $"/flight/BookingConfirmed/{cartDto}",
                    CancelUrl = domain + "/PaymentCancelled"
                },
                PurchaseUnits = new List<PurchaseUnitRequest>
            {
                new PurchaseUnitRequest
                {
                    AmountWithBreakdown = new AmountWithBreakdown
                    {
                        CurrencyCode = paymentRequest.Currency,
                        Value = paymentRequest.Amount.ToString("0.00")
                    },
                    Description = paymentRequest.Description,
                    ShippingDetail = new ShippingDetail
                    {
                        AddressPortable = new AddressPortable
                        {                       
                            AdminArea1 = "CA",
                            PostalCode = "90011",
                            CountryCode = "US"
                        }
                    }
                }
            }
            };
        }
        public class PayPalPaymentRequest
        {
            public decimal Amount { get; set; } // The amount to be paid
            public string Currency { get; set; } // The currency of the payment (e.g., USD)
            public string Description { get; set; } // Description of the payment
                                                    // Add more properties as needed, such as shipping information, item details, etc.
        }
    }

}
