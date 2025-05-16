using System.Collections.Generic;
using System.Threading.Tasks;
using org.wakanda.framework.Entity;

namespace org.wakanda.framework.Services.Interface
{
    public interface IAuditTrailService
    {
        Task LogAuditAsync(AuditTrail auditTrail); 
        Task LogAuditAsync(IEnumerable<AuditTrail> auditTrails); 
    }
}