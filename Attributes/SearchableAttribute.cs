using System;

namespace org.wakanda.framework.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class SearchableAttribute : Attribute { }


[AttributeUsage(AttributeTargets.Property)]
public class SortableAttribute : Attribute { }
