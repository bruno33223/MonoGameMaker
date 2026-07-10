using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MonoGameMaker.IDE.Core
{
    public static class RoomSerializer
    {
        public class RoomInstance
        {
            public string spriteName { get; set; } = string.Empty;
            public float x { get; set; }
            public float y { get; set; }
        }

        public static List<RoomInstance> LoadRoom(string projectRoot, Action<string> logCallback)
        {
            string jsonPath = Path.Combine(projectRoot, "Content", "Rooms", "room_init.json");
            return LoadRoomPath(jsonPath, logCallback);
        }

        public static List<RoomInstance> LoadRoomPath(string jsonPath, Action<string> logCallback)
        {
            try
            {
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var list = JsonSerializer.Deserialize<List<RoomInstance>>(jsonContent);
                    return list ?? new List<RoomInstance>();
                }
            }
            catch (Exception ex)
            {
                logCallback($"Error loading room configuration: {ex.Message}");
            }
            return new List<RoomInstance>();
        }

        public static bool SaveRoom(string projectRoot, List<RoomInstance> instances, Action<string> logCallback)
        {
            string jsonPath = Path.Combine(projectRoot, "Content", "Rooms", "room_init.json");
            return SaveRoomPath(jsonPath, instances, logCallback);
        }

        public static bool SaveRoomPath(string jsonPath, List<RoomInstance> instances, Action<string> logCallback)
        {
            try
            {
                string? dir = Path.GetDirectoryName(jsonPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonContent = JsonSerializer.Serialize(instances, options);
                
                File.WriteAllText(jsonPath, jsonContent);
                logCallback($"Saved room configuration to {jsonPath}");
                return true;
            }
            catch (Exception ex)
            {
                logCallback($"Error saving room configuration: {ex.Message}");
                return false;
            }
        }
    }
}
