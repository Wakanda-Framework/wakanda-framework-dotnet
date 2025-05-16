using System;
using Microsoft.Extensions.Localization;

namespace org.wakanda.framework.Extensions
{
    public static class EnumExtensions
    {
        public static string GetLocalizedDescription(this Enum value, IStringLocalizer localizer)
        {
            if (localizer == null)
                throw new ArgumentNullException(nameof(localizer));

            var resourceKey = $"ErrorType_{value}";
            var localized = localizer[resourceKey];
            
            return !localized.ResourceNotFound 
                ? localized.Value 
                : value.ToString();
        }
    }
}