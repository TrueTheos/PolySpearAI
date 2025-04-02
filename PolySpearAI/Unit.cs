using System.Text.Json;
using System.Text.Json.Serialization;
using static PolySpearAI.HexGrid;

namespace PolySpearAI
{
    public record Unit
    {
        public string ID { get; set; }
        [JsonIgnore] private int _id;
        public enum PLAYER { ELF, ORC }
        [JsonIgnore] public PLAYER Player;
        public List<WEAPON> Items { get; set; }
        public SIDE Rotation;

        [JsonConstructor]
        public Unit()
        {

        }

        public WEAPON GetItemOnSide(int side)
        {
            int effectiveIndex = (((int)side - (int)Rotation) + 6) % 6;
            return Items[effectiveIndex];
        }

        public override int GetHashCode()
        {
            return _id;
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum WEAPON
        {
            EMPTY = 0,
            ATTACK_SHIELD = 1, //a
            AXE = 2, //x
            BOW = 3, //b
            FIST = 4, //f
            MACE = 5, //m
            PUSH = 6, //p
            STRONG_AXE = 7, //X
            STRONG_SHIELD = 8, //H
            SHIELD = 9, //h
            SPEAR = 10, //s
            STAFF = 11, //t
            SWORD = 12 //w
        }
    }
}
