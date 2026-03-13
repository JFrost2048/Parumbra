using System;

public enum RoomKind
{
    Start,
    ExitZone,
    Combat,
    Event,
    Search,
    Supply,
    KeyRoom
}

[Flags]
public enum RoomStateFlags
{
    None = 0,
    PowerOn = 1 << 0,
    OnFire = 1 << 1,
    Sealed = 1 << 2,
    HazardGas = 1 << 3
}
