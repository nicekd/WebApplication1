using System.Threading.Tasks;
using WebApplication1.Model;

namespace WebApplication1.Services
{
    public interface IAuditLogService
    {
        Task LogActionAsync(string userId, string action, string ipAddress);
    }
}
