using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using org.wakanda.framework.Utilities;
using Swashbuckle.AspNetCore.Annotations; 

namespace org.wakanda.framework.Entity
{
    public abstract class BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual int Id { get; set; }
        public virtual long DateCreated { get; set; } = UnixTimeHelper.CurrentUnixTimestamp();
        public virtual long DateModified { get; set; } // Unix timestamp
        public virtual string? CreatedBy { get; set; }
        public virtual string? CreatedByIp { get; set; }
        public virtual string? ModifiedBy { get; set; }
        public virtual string? ModifiedByIp { get; set; }
        public virtual bool IsDeleted { get; set; } = false;

        [DefaultValue(0)]
        public virtual int Version { get; set; } = 0;
        
        [DefaultValue("A")]
        public virtual string Status { get; set; } = "A";

        // Soft Delete: Marks entity as deleted.
        public virtual void MarkAsDeleted(string modifiedBy, string modifiedByIp)
        {
            IsDeleted = true;
            DateModified = UnixTimeHelper.CurrentUnixTimestamp();
            ModifiedBy = modifiedBy;
            ModifiedByIp = modifiedByIp;
        }

        // Enable Entity.
        public virtual void Enable()
        {
            Status = "A";
            DateModified = UnixTimeHelper.CurrentUnixTimestamp();
        }

        // Disable Entity.
        public virtual void Disable()
        {
            Status = "D";
            DateModified = UnixTimeHelper.CurrentUnixTimestamp();
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            if (obj == null || this.GetType() != obj.GetType())
                return false;

            var other = (BaseEntity)obj;

            // Compare DateCreated (non-nullable)
            if (! this.DateCreated.Equals(other.DateCreated))
                return false;

            // Compare DateModified (non-nullable)
            // Since DateModified is non-nullable, we don't need HasValue checks
            if (! this.DateModified.Equals(other.DateModified))
                return false;

            if (this.Version != other.Version)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, DateCreated, DateModified, Version);
        }
    }
}