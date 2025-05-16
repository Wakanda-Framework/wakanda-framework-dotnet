using System.Collections.Generic;
using System.ComponentModel;

namespace org.wakanda.framework.Utilities
{
    public abstract class BaseFilter
    {
        [DefaultValue(1)]
        public int PageNo { get; set; } = 1;

        [DefaultValue(10)]
        public int PageSize { get; set; } = 10;

        [DefaultValue("Id")]
        public string SortBy { get; set; } = "Id";

        [DefaultValue("asc")]
        public string SortDirection { get; set; } = "asc";

        [DefaultValue(null)]
        public string? SearchTerm { get; set; }

        [DefaultValue(null)]
        public List<string>? SearchProperties { get; set; }
    }
}
