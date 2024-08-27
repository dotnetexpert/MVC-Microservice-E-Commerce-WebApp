namespace WebApplicationUI.Models
{
    public class PayPalRequestDto
    {
        public string ApprovedUrl { get; set; }
        public string CancelUrl { get; set; }
        public OrderHeaderDto OrderHeader { get; set; }

    }
}
