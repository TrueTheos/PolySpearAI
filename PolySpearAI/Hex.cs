using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolySpearAI
{
    [Serializable]
    public class Hex
    {
        public int Q { get; } // Column
        public int R { get; } // Row

        private int _id;

        public Hex(int q, int r)
        {
            Q = q;
            R = r;
            _id = HashCode.Combine(Q, R);
        }

        public override int GetHashCode()
        {
            return _id;
        }
    }
}
