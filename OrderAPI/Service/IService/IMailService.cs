using OrderAPI.Utility;

namespace OrderAPI.Service.IService
{
    public interface IMailService
    {
        Task SendEmailAsync(MailRequest mailRequest);
    }
}
