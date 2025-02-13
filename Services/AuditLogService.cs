using System;
using System.Threading.Tasks;
using WebApplication1.Model;

namespace WebApplication1.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AuthDbContext _dbContext;

        public AuditLogService(AuthDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task LogActionAsync(string userId, string action, string ipAddress)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                Timestamp = DateTime.UtcNow,
                IPAddress = ipAddress
            };

            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync();
        }
    }
}
