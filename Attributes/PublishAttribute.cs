using Action = org.wakanda.framework.Enums.Action;
using System;

namespace org.wakanda.framework.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PublishAttribute : Attribute
{
    public Action PublishOn { get; }

    public PublishAttribute(Action publishOn = Action.All)
    {
        PublishOn = publishOn;
    }
}