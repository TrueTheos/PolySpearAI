using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PolySpearAI.Program;

namespace PolySpearAI
{
    public record Unit
    {
        public string ID { get; set; }
        [JsonIgnore] public PLAYER Player;
        public List<Weapon> Items { get; set; }
        public Side Rotation;
        [JsonIgnore] public int Q { get; private set; }
        [JsonIgnore] public int R { get; private set; }
  
        public Unit(string id, List<Weapon> items, Side rotation)
        {
            ID = id;
            Items = items;
            Rotation = rotation;
        } 

        public void SetPosition(Hex position)
        {
            SetPosition(position.Q, position.R);
        }

        public void SetPosition(int q, int r)
        {
            Q = q;
            R = r;
        }

        public Weapon GetItemOnSide(int side)
        {
            int effectiveIndex = (((int)side - (int)Rotation) + 6) % 6;
            return Items[effectiveIndex];
        }
    }
}
