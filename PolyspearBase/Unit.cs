using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolyspearLib
{
    public record Unit
    {
        public string ID { get; set; }
        [JsonIgnore] private int _id;
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
    }
}
