using System;

namespace org.wakanda.framework.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CacheableAttribute : Attribute
    {
        /// <summary>
        /// Indicates whether this entity type should be cached.
        /// </summary>
        public bool EnableCaching { get; }

        public CacheableAttribute(bool enableCaching = true)
        {
            EnableCaching = enableCaching;
        }
    }
}