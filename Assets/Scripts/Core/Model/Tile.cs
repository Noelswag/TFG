using System;

namespace Core.Model
{
    public class Tile : ITile
    {
        public GridPosition Position { get; }
        public TerrainType CurrentTerrain { get; private set; }
        public ITerrainProperties Properties => TerrainPropertiesRegistry.GetProperties(CurrentTerrain);

        public event Action<ITile, TerrainType, TerrainType> OnTerrainChanged;

        /// <summary>
        /// Initializes a new instance of the Tile class.
        /// </summary>
        public Tile(GridPosition position, TerrainType initialTerrain)
        {
            Position = position;
            CurrentTerrain = initialTerrain;
        }

        /// <summary>
        /// Sets the terrain type of the tile and triggers the OnTerrainChanged event.
        /// </summary>
        public void SetTerrain(TerrainType newTerrain)
        {
            if (CurrentTerrain == newTerrain) return;

            TerrainType oldTerrain = CurrentTerrain;
            CurrentTerrain = newTerrain;

            OnTerrainChanged?.Invoke(this, oldTerrain, newTerrain);
        }
    }
}
