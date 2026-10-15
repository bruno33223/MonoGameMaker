using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace TestConsole.Tests.Domain
{
    public class PlayerProfileModel
    {
        public long TotalSouls { get; set; } = 0;
        public List<string> CollectedBlueprints { get; set; } = new List<string>();
        public HashSet<string> UnlockedBlueprints { get; set; } = new HashSet<string>();
        public string LastVisitedLab { get; set; } = "ResearchLab_Sys1";
        public int SystemId { get; set; } = 1;

        public float HullHealth { get; set; } = 100f;
        public float MaxHull { get; set; } = 100f;
        public float ShieldHealth { get; set; } = 50f;
        public float MaxShield { get; set; } = 50f;
    }

    public class LostSoulStashEntity
    {
        public Vector2 Position { get; set; }
        public int SectorId { get; set; }
        public long SoulsAmount { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class MetagameService
    {
        public LostSoulStashEntity? CurrentStash { get; private set; }

        public void ProcessPlayerDeath(PlayerProfileModel profile, Vector2 deathPos, int sectorId)
        {
            // 1. Cria ou sobrescreve stash com 100% das almas na coordenada exata
            CurrentStash = new LostSoulStashEntity
            {
                Position = deathPos,
                SectorId = sectorId,
                SoulsAmount = profile.TotalSouls,
                IsActive = true
            };

            // 2. Zera saldo imediato
            profile.TotalSouls = 0;

            // 3. Esvazia mochila de blueprints da run, preservando catálogo permanente
            profile.CollectedBlueprints.Clear();

            // 4. Respawn no último laboratório com 100% de integridade
            profile.HullHealth = profile.MaxHull;
            profile.ShieldHealth = profile.MaxShield;

            // 5. Injeção de emergência de 300 almas se SystemId >= 3
            if (profile.SystemId >= 3 && profile.TotalSouls == 0)
            {
                profile.TotalSouls = 300;
            }
        }

        public bool RecoverLostSoulStash(PlayerProfileModel profile, Vector2 playerPos)
        {
            if (CurrentStash != null && CurrentStash.IsActive)
            {
                if (Vector2.Distance(playerPos, CurrentStash.Position) < 40f)
                {
                    profile.TotalSouls += CurrentStash.SoulsAmount;
                    CurrentStash.IsActive = false;
                    return true;
                }
            }
            return false;
        }
    }

    public class SaveManagerModel
    {
        private readonly string _baseDirectory;

        public SaveManagerModel(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            Directory.CreateDirectory(_baseDirectory);
        }

        public string GetSlotPath(int slotIndex) => Path.Combine(_baseDirectory, $"save_slot_{slotIndex}.json");
        public string GetMetaPath() => Path.Combine(_baseDirectory, "save_meta.json");

        public void SaveAtomic(int slotIndex, object data)
        {
            string targetPath = GetSlotPath(slotIndex);
            string tempPath = targetPath + ".tmp";

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tempPath, json);

            // Substituição atômica via File.Move / Replace
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            File.Move(tempPath, targetPath);
        }

        public void SaveMetaAtomic(object metaData)
        {
            string targetPath = GetMetaPath();
            string tempPath = targetPath + ".tmp";

            string json = JsonSerializer.Serialize(metaData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tempPath, json);

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            File.Move(tempPath, targetPath);
        }
    }
}
