using System.Collections.Generic;
using System.Threading.Tasks;
using org.wakanda.framework.Entity;
using org.wakanda.framework.Repositories.Interface;
using org.wakanda.framework.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace org.wakanda.framework.Services
{
    public class AuditTrailService<TContext> : IAuditTrailService
        where TContext : DbContext
    {
        private readonly TContext _context;

        public AuditTrailService(TContext context)
        {
            _context = context;
        }

        // Implement single audit log method
        public async Task LogAuditAsync(AuditTrail auditTrail)
        {
            await _context.Set<AuditTrail>().AddAsync(auditTrail);
            await _context.SaveChangesAsync();
        }

        public async Task LogAuditAsync(IEnumerable<AuditTrail> auditTrails)
        {
            // Note: Set<AuditTrail>() will work so long as the consuming app has
            // added DbSet<AuditTrail> (or called modelBuilder.Entity<AuditTrail>).
            await _context.Set<AuditTrail>().AddRangeAsync(auditTrails);
            await _context.SaveChangesAsync();
        }
    }
}