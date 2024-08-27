using AuthAPI.Data;
using AuthAPI.Models;
using AuthAPI.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Service
{
    public class AuditLogService : IAuditLogService
    {
        private ApplicationDbContext _context;
        public AuditLogService(ApplicationDbContext dbContext)
        {
            _context = dbContext;
        }

        public async Task LogAsync(AuditLog auditLog)
        {
            try
            {
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging audit log: {ex.Message}");
                throw;
            }
        }
    }
}
