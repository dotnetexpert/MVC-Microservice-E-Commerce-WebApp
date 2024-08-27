using AuthAPI.Models;

namespace AuthAPI.Service.IService
{
    public interface IAuditLogService
    {
        Task LogAsync(AuditLog auditLog);
    }
}
