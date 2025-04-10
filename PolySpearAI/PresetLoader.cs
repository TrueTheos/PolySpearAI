﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static PolySpearAI.HexGrid;
using static PolySpearAI.Unit;

namespace PolySpearAI
{
    public static class PresetLoader
    {
        public static UnitPreset LoadPresets(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var presets = JsonSerializer.Deserialize<UnitPreset>(json);
            return presets;
        }

        public static void SaveUnit(Unit unit)
        {
            UnitPreset preset;

            if (File.Exists(Program.PRESET_FILE_PATH))
            {
                string json = File.ReadAllText(Program.PRESET_FILE_PATH);
                preset = JsonSerializer.Deserialize<UnitPreset>(json);
                if (preset == null || preset.Units == null)
                {
                    preset = new UnitPreset();
                }
            }
            else
            {
                preset = new UnitPreset();
            }

            preset.Units.Add(unit);

            string outputJson = JsonSerializer.Serialize(preset);
            File.WriteAllText(Program.PRESET_FILE_PATH, outputJson);
        }
    }
}
