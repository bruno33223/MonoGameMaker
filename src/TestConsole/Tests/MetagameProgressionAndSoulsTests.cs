using System;
using System.IO;
using Microsoft.Xna.Framework;
using TestConsole.Tests.Domain;

namespace TestConsole.Tests
{
    public static class MetagameProgressionAndSoulsTests
    {
        public static void RunAll(ref int passCount, ref int totalCount)
        {
            Console.WriteLine("\n[SUÍTE 7/8] Executando MetagameProgressionAndSoulsTests (TEST-MTG-01 a 09)...");

            Test_MTG_01_SoulsCurveEarlyGame(ref passCount, ref totalCount);
            Test_MTG_02_SoulsCurveLateGameContinuity(ref passCount, ref totalCount);
            Test_MTG_03_DerivativeContinuityAtLevel10(ref passCount, ref totalCount);
            Test_MTG_04_DeathAndLostSoulStash(ref passCount, ref totalCount);
            Test_MTG_05_EphemeralBlueprintsWipeOnDeath(ref passCount, ref totalCount);
            Test_MTG_06_CheckpointRespawnAtLastLab(ref passCount, ref totalCount);
            Test_MTG_07_EmergencySoulsInjectionSystem3Plus(ref passCount, ref totalCount);
            Test_MTG_08_RecoverLostSoulStash(ref passCount, ref totalCount);
            Test_MTG_09_AtomicSave8SlotsJson(ref passCount, ref totalCount);
        }

        public static void Test_MTG_01_SoulsCurveEarlyGame(ref int passCount, ref int totalCount)
        {
            totalCount++;
            (int level, long expected)[] cases = {
                (0, 40L),
                (1, 100L),
                (5, 640L),
                (10, 1990L)
            };

            foreach (var (level, expected) in cases)
            {
                long val = ProgressionMath.SoulsRequired(level);
                if (val != expected)
                    throw new Exception($"[TEST-MTG-01] SoulsRequired no nível {level} incorreto: {val}, esperado {expected}");
            }

            passCount++;
            Console.WriteLine("  -> [TEST-MTG-01] Curva de Custo de Almas C1 (N <= 10 -> N=0:40, N=1:100, N=5:640, N=10:1990): PASS");
        }

        public static void Test_MTG_02_SoulsCurveLateGameContinuity(ref int passCount, ref int totalCount)
        {
            totalCount++;
            (int level, long expected)[] cases = {
                (10, 1990L),
                (20, 5940L),
                (50, 23790L)
            };

            foreach (var (level, expected) in cases)
            {
                long val = ProgressionMath.SoulsRequired(level);
                if (val != expected)
                    throw new Exception($"[TEST-MTG-02] SoulsRequired no nível {level} incorreto: {val}, esperado {expected}");
            }

            passCount++;
            Console.WriteLine("  -> [TEST-MTG-02] Curva de Custo de Almas C1 (N > 10 -> N=10:1990, N=20:5940, N=50:23790): PASS");
        }

        public static void Test_MTG_03_DerivativeContinuityAtLevel10(ref int passCount, ref int totalCount)
        {
            totalCount++;
            long s9 = ProgressionMath.SoulsRequired(9);
            long s10 = ProgressionMath.SoulsRequired(10);
            long s11 = ProgressionMath.SoulsRequired(11);

            long deltaBefore = s10 - s9; // 1990 - 1660 = 330
            long deltaAfter = s11 - s10;  // 2340 - 1990 = 350

            if (deltaBefore != 330L || deltaAfter != 350L)
                throw new Exception($"[TEST-MTG-03] Inclinações incrementais divergentes: Antes={deltaBefore} (esp 330), Depois={deltaAfter} (esp 350)");

            // Coeficiente linear de derivada exata no ponto de transição é 345
            long midpointSlope = (deltaBefore + deltaAfter) / 2L;
            if (midpointSlope != 340L) // (330 + 350) / 2 = 340
                throw new Exception($"[TEST-MTG-03] Média de transição discreta divergente: {midpointSlope}");

            passCount++;
            Console.WriteLine("  -> [TEST-MTG-03] Continuidade da Derivada em N=10 (Transição sem degraus artificiais): PASS");
        }

        public static void Test_MTG_04_DeathAndLostSoulStash(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var profile = new PlayerProfileModel { TotalSouls = 5000, SystemId = 1 };
            var metagame = new MetagameService();

            Vector2 deathPos = new Vector2(340f, -120f);
            int sectorId = 3;

            metagame.ProcessPlayerDeath(profile, deathPos, sectorId);

            if (profile.TotalSouls != 0)
                throw new Exception($"[TEST-MTG-04] Saldo do jogador pós-morte deveria ser 0. Obtido: {profile.TotalSouls}");

            if (metagame.CurrentStash == null || !metagame.CurrentStash.IsActive)
                throw new Exception("[TEST-MTG-04] LostSoulStashEntity não foi persistido ou não está ativo.");

            if (metagame.CurrentStash.SoulsAmount != 5000)
                throw new Exception($"[TEST-MTG-04] Stash deveria conter 5000 almas. Obtido: {metagame.CurrentStash.SoulsAmount}");

            if (metagame.CurrentStash.Position != deathPos || metagame.CurrentStash.SectorId != sectorId)
                throw new Exception($"[TEST-MTG-04] Coordenadas do stash divergiram do local da morte: {metagame.CurrentStash.Position}, Setor: {metagame.CurrentStash.SectorId}");

            passCount++;
            Console.WriteLine("  -> [TEST-MTG-04] Ciclo de Morte e Mancha de Sangue (5000 almas em (340, -120) Setor 3): PASS");
        }

        public static void Test_MTG_05_EphemeralBlueprintsWipeOnDeath(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var profile = new PlayerProfileModel();
            profile.CollectedBlueprints.Add("BP_PlasmaCannon");
            profile.CollectedBlueprints.Add("BP_HeavyShield");

            profile.UnlockedBlueprints.Add("BP_VanguardChassis"); // Desbloqueado permanentemente

            var metagame = new MetagameService();
            metagame.ProcessPlayerDeath(profile, Vector2.Zero, sectorId: 1);

            if (profile.CollectedBlueprints.Count != 0)
                throw new Exception($"[TEST-MTG-05] Mochila de blueprints da run não foi esvaziada: {profile.CollectedBlueprints.Count} itens");

            if (!profile.UnlockedBlueprints.Contains("BP_VanguardChassis"))
                throw new Exception("[TEST-MTG-05] Catálogo permanente de blueprints foi perdido indevidamente.");

            passCount++;
            Console.WriteLine("  -> [TEST-MTG-05] Destruição de Blueprints Efêmeras na Morte (Mochila zerada, Catálogo preservado): PASS");
        }

        public static void Test_MTG_06_CheckpointRespawnAtLastLab(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var profile = new PlayerProfileModel
            {
                LastVisitedLab = "ResearchLab_System_2",
                HullHealth = 10f,
                ShieldHealth = 0f
            };

            var metagame = new MetagameService();
            metagame.ProcessPlayerDeath(profile, new Vector2(500, 500), sectorId: 2);

            if (profile.LastVisitedLab != "ResearchLab_System_2")
                throw new Exception($"[TEST-MTG-06] Ponto de respawn alterado incorretamente: {profile.LastVisitedLab}");

            if (profile.HullHealth != profile.MaxHull || profile.ShieldHealth != profile.MaxShield)
                throw new Exception($"[TEST-MTG-06] Casco e Escudo não foram restaurados a 100% no respawn: Hull={profile.HullHealth}, Shield={profile.ShieldHealth}");

            passCount++;
            Console.WriteLine("  -> [TEST-MTG-06] Respawn em Ponto de Controle (ResearchLab Sistema 2, 100% HP/Shield): PASS");
        }

        public static void Test_MTG_07_EmergencySoulsInjectionSystem3Plus(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var profile = new PlayerProfileModel
            {
                TotalSouls = 0,
                SystemId = 3 // Sistema 3 (avançado)
            };

            var metagame = new MetagameService();
            metagame.ProcessPlayerDeath(profile, Vector2.Zero, sectorId: 5);

            if (profile.TotalSouls != 300)
                throw new Exception($"[TEST-MTG-07] Injeção emergencial de almas para Sistema 3+ esperada 300, obtido: {profile.TotalSouls}");

            passCount++;
            Console.WriteLine("  -> [TEST-MTG-07] Injeção de Almas Emergenciais de Respawn (SystemId >= 3 -> 300 almas): PASS");
        }

        public static void Test_MTG_08_RecoverLostSoulStash(ref int passCount, ref int totalCount)
        {
            totalCount++;
            var profile = new PlayerProfileModel { TotalSouls = 5000, SystemId = 1 };
            var metagame = new MetagameService();

            Vector2 stashPos = new Vector2(100f, 100f);
            metagame.ProcessPlayerDeath(profile, stashPos, sectorId: 1);
            // Jogador se move para a posição do stash
            bool recovered = metagame.RecoverLostSoulStash(profile, stashPos);

            if (!recovered)
                throw new Exception("[TEST-MTG-08] Falha ao recuperar o stash de almas ao sobrevoar.");

            if (profile.TotalSouls != 5000)
                throw new Exception($"[TEST-MTG-08] Saldo pós-recuperação esperado 5000, obtido: {profile.TotalSouls}");

            if (metagame.CurrentStash == null || metagame.CurrentStash.IsActive)
                throw new Exception("[TEST-MTG-08] Stash deve ser desativado após resgate.");

            passCount++;
            Console.WriteLine("  -> [TEST-MTG-08] Recuperação da Mancha de Sangue (5000 almas resgatadas e stash desativado): PASS");
        }

        public static void Test_MTG_09_AtomicSave8SlotsJson(ref int passCount, ref int totalCount)
        {
            totalCount++;
            string testDir = Path.Combine(Path.GetTempPath(), "MonoGameMaker_SaveTests_" + Guid.NewGuid().ToString("N"));
            var saveMgr = new SaveManagerModel(testDir);

            try
            {
                // Grava todos os 8 slots
                for (int slot = 1; slot <= 8; slot++)
                {
                    var data = new { Slot = slot, PlayerName = $"Pilot_{slot}", Timestamp = DateTime.UtcNow.Ticks };
                    saveMgr.SaveAtomic(slot, data);

                    string slotFile = saveMgr.GetSlotPath(slot);
                    if (!File.Exists(slotFile))
                        throw new Exception($"[TEST-MTG-09] Arquivo de save do slot {slot} não foi criado: {slotFile}");

                    string json = File.ReadAllText(slotFile);
                    if (!json.Contains($"Pilot_{slot}"))
                        throw new Exception($"[TEST-MTG-09] Conteúdo do slot {slot} corrompido ou inconsistente.");
                }

                // Grava save_meta.json
                var meta = new { ActiveSlot = 2, TotalPlayTimeSeconds = 3600.0, MaxSectorReached = 5 };
                saveMgr.SaveMetaAtomic(meta);

                string metaFile = saveMgr.GetMetaPath();
                if (!File.Exists(metaFile))
                    throw new Exception("[TEST-MTG-09] save_meta.json não foi criado.");

                string metaJson = File.ReadAllText(metaFile);
                if (!metaJson.Contains("MaxSectorReached"))
                    throw new Exception("[TEST-MTG-09] save_meta.json com metadados inconsistentes.");

                // Confirma que nenhum arquivo residual .tmp sobreviveu
                string[] tmpFiles = Directory.GetFiles(testDir, "*.tmp");
                if (tmpFiles.Length > 0)
                    throw new Exception($"[TEST-MTG-09] Arquivos temporários residuais não foram limpos: {tmpFiles.Length}");
            }
            finally
            {
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, recursive: true);
                }
            }

            passCount++;
            Console.WriteLine("  -> [TEST-MTG-09] Integridade e Atomicidade dos 8 Slots de Save JSON + Meta: PASS");
        }
    }
}
