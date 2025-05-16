using System;

namespace org.wakanda.framework.Enums;

[Flags]
public enum Action : short
{
    None = 0x0000,
    Read = 0x0001,
    Create = 0x0002,
    Update = 0x0004,
    Delete = 0x0008,
    
    All = Read | Create | Update | Delete
}
