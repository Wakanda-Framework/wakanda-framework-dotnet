using org.wakanda.framework.Utilities;
using System.ComponentModel;

namespace org.wakanda.framework.BaseDto
{
    public abstract class BaseDto
    {
        public virtual int Id { get; set; }
        public virtual long DateCreated { get; set; } = UnixTimeHelper.CurrentUnixTimestamp();
        public virtual long DateModified { get; set; } // Unix timestamp
        public virtual string? CreatedBy { get; set; }
        public virtual string? CreatedByIp { get; set; }
        public virtual string? ModifiedBy { get; set; }
        public virtual string? ModifiedByIp { get; set; }
        public virtual bool IsDeleted { get; set; } = false;

        [DefaultValue(1)]
        public virtual int Version { get; set; } = 1;

        [DefaultValue("A")]
        public virtual string Status { get; set; } = "A";
    }
}
