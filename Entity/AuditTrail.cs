
using org.wakanda.framework.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System;

namespace org.wakanda.framework.Entity
{
public class AuditTrail : BaseEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public override int Id { get; set; }
    public string EntityType { get; set; }
    public int EntityId { get; set; }
    public string? Action { get; set; }
    public string? UserId { get; set; }
    public string? UserIp { get; set; }
    public string? OldValues { get; set; } 
    public string? NewValues { get; set; } 
    public DateTime Timestamp { get; set; }
}
}



