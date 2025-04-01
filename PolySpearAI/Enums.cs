using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolySpearAI
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum Side
    {
        UP_RIGHT = 0,
        RIGHT = 1,
        DOWN_RIGHT = 2,
        DOWN_LEFT = 3,
        LEFT = 4,
        UP_LEFT = 5
    }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum Weapon
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

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum PLAYER { ELF, ORC }
}
