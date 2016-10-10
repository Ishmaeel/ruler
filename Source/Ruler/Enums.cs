using System;

namespace Ruler
{
    [Flags]
    enum ResizeRegion
    {
        None = 0,
        N = 1,
        E = 2,
        S = 4,
        W = 8,
        NE = N + E,
        SE = S + E,
        SW = S + W,
        NW = N + W
    }

    enum DragMode
    {
        None,
        Move,
        Resize
    }
}
