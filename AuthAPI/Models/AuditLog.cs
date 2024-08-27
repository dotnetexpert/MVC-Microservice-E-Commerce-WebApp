namespace AuthAPI.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; }
        public string IPAddress { get; set; }
        public string AdditionalInfo { get; set; }
        public string BrowserInfo { get; set; }
        public string Counrty { get; set; }
    }
}
