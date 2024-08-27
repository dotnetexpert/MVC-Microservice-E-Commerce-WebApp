namespace OrderAPI.Models.Dto
{
    public class PayPalRequestDto
    {
        public string ApprovedUrl { get; set; }
        public string CancelUrl { get; set; }
        public OrderHeaderDto OrderHeader { get; set; }
    }
}
