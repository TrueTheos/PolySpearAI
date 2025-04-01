using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PolySpearAI.Program;

namespace PolySpearAI
{
    public class Unit
    {
        public string ID { get; set; }
        [JsonIgnore] public PLAYER Player;
        public List<Weapon> Items { get; set; }
        public Side Rotation { get; set; }
        [JsonIgnore] public int Q { get; private set; }
        [JsonIgnore] public int R { get; private set; }

        public Unit()
        {

        }

        public Unit(string iD, List<Weapon> items, Side rotation)
        {
            ID = iD;
            Items = items;
            Rotation = rotation;
        }

        public Unit(Unit reference)
        {
            ID = reference.ID;
            Player = reference.Player;
            Items = new List<Weapon>(reference.Items);
            Rotation = reference.Rotation;
        }

        public Unit Clone()
        {
            return new Unit(this);
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

        public void SetRotation(Side newRotation)
        {
            Rotation = newRotation;
        }

        public override string ToString() => $"{ID} (P{Player}) Rot:{Rotation}";
    }
}
