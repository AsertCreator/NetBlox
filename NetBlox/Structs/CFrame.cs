using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NetBlox.Structs
{
    public struct CFrame
    {
        public Vector3 Position { get; set; }

        public static CFrame operator +(CFrame a, CFrame b) => new () { Position = a.Position + b.Position };
        public static CFrame operator -(CFrame a, CFrame b) => new () { Position = a.Position - b.Position };
        public static CFrame operator *(CFrame a, CFrame b) => new () { Position = a.Position * b.Position };
        public static CFrame operator /(CFrame a, CFrame b) => new () { Position = a.Position / b.Position };
    }
}
