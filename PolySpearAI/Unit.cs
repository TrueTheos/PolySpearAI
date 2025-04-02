using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolySpearAI
{
    public record Unit
    {
        public string ID { get; set; }
        [JsonIgnore] private int _id;
        [JsonIgnore] public PLAYER Player;
        public List<Weapon> Items { get; set; }
        public Side Rotation;

        [JsonConstructor]
        public Unit()
        {

        }
  
        public Unit(Side rotation, string id, List<Weapon> items)
        {
            ID = id;
            _id = HashCode.Combine(id);
            Items = items;
            Rotation = rotation;
        } 

        public Weapon GetItemOnSide(int side)
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
