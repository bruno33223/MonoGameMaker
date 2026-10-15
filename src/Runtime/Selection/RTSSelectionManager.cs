using System;
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Resolution;
using MonoGameMaker.Runtime.Spatial;

namespace MonoGameMaker.Runtime.Selection
{
    /// <summary>
    /// Gerenciador tático de seleção por clique e Marquee Box,
    /// com compensação de viewport virtual 1080p, câmera inversa e priorização militar.
    /// </summary>
    public sealed class RTSSelectionManager
    {
        private readonly SpatialHashGrid _spatialGrid;
        private readonly int[] _selectedIds;
        private int _selectedCount;
        private readonly int[] _queryBuffer;

        public ReadOnlySpan<int> SelectedEntities => _selectedIds.AsSpan(0, _selectedCount);
        public int SelectedCount => _selectedCount;

        public RTSSelectionManager(SpatialHashGrid spatialGrid, int maxSelection = 512)
        {
            ArgumentNullException.ThrowIfNull(spatialGrid);
            _spatialGrid = spatialGrid;
            _selectedIds = new int[maxSelection];
            _queryBuffer = new int[maxSelection];
        }

        public void ClearSelection() => _selectedCount = 0;

        /// <summary>
        /// Seleciona unidades dentro da caixa elástica convertendo coordenadas de tela física
        /// para o espaço de mundo via ResolutionManager e Camera2D Inversa.
        /// </summary>
        public void SelectBox(
            Vector2 screenStart, 
            Vector2 screenEnd, 
            Matrix cameraTransform, 
            ReadOnlySpan<byte> factions, 
            ReadOnlySpan<UnitTacticalRole> roles, 
            byte playerFaction, 
            bool isAdditive)
        {
            // 1. Converte Tela Física para Espaço Virtual 1920x1080
            Vector2 virtStart = ResolutionManager.ScreenToVirtual(screenStart, clampToVirtualBounds: true);
            Vector2 virtEnd = ResolutionManager.ScreenToVirtual(screenEnd, clampToVirtualBounds: true);

            // 2. Inverte a Câmera para Espaço de Mundo
            Matrix invCam = Matrix.Invert(cameraTransform);
            Vector2 worldA = Vector2.Transform(virtStart, invCam);
            Vector2 worldB = Vector2.Transform(virtEnd, invCam);

            var box = new RectangleF(worldA, worldB);

            if (!isAdditive) ClearSelection();

            // 3. Consulta Espacial AABB Zero-GC
            int found = _spatialGrid.QueryBox(box, _queryBuffer);

            // 4. Filtragem Militar: se houver combatentes, descarta trabalhadores
            bool hasCombat = false;
            for (int i = 0; i < found; i++)
            {
                int id = _queryBuffer[i];
                if (factions[id] == playerFaction && roles[id] == UnitTacticalRole.Combat)
                {
                    hasCombat = true;
                    break;
                }
            }

            for (int i = 0; i < found && _selectedCount < _selectedIds.Length; i++)
            {
                int id = _queryBuffer[i];
                if (factions[id] != playerFaction) continue; // Apenas unidades aliadas na caixa

                if (hasCombat && roles[id] == UnitTacticalRole.Worker) continue; // Prioridade militar

                if (!_selectedIds.AsSpan(0, _selectedCount).Contains(id))
                {
                    _selectedIds[_selectedCount++] = id;
                }
            }
        }

        /// <summary>
        /// Seleção por clique único pontual com raio de tolerância de clique.
        /// </summary>
        public void SelectPoint(
            Vector2 screenPoint,
            Matrix cameraTransform,
            ReadOnlySpan<byte> factions,
            ReadOnlySpan<UnitTacticalRole> roles,
            byte playerFaction,
            bool isAdditive,
            float pickRadius = 8f)
        {
            SelectBox(
                screenPoint - new Vector2(pickRadius),
                screenPoint + new Vector2(pickRadius),
                cameraTransform,
                factions,
                roles,
                playerFaction,
                isAdditive
            );
        }

        /// <summary>
        /// Despacha uma ordem/comando para todas as entidades atualmente selecionadas.
        /// </summary>
        public void IssueCommand(UnitCommand command, Action<int, UnitCommand> commandDispatcher)
        {
            ArgumentNullException.ThrowIfNull(commandDispatcher);
            for (int i = 0; i < _selectedCount; i++)
            {
                commandDispatcher(_selectedIds[i], command);
            }
        }
    }
}
