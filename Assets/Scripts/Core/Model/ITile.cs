using System;

namespace Core.Model
{
    public interface ITile
    {
        GridPosition Position { get; }
        TerrainType CurrentTerrain { get; }
        ITerrainProperties Properties { get; }

        /// <summary>
        /// Modifies the terrain type of the tile.
        /// </summary>
        void SetTerrain(TerrainType newTerrain);

        event Action<ITile, TerrainType, TerrainType> OnTerrainChanged;
    }
}
