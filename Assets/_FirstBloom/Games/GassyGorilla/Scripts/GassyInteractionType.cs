using System;

namespace FirstBloom.Games.GassyGorilla
{
    [Flags]
    public enum GassyInteractionType
    {
        None = 0,
        ThornDodge = 1 << 0,
        GeyserDodge = 1 << 1,
        SapEscape = 1 << 2,
        UpdraftRide = 1 << 3,
        BounceBloom = 1 << 4
    }
}
