using System;

namespace Core.Model
{
    public class TileGrid : ITileGrid
    {
        public int Width { get; }
        public int Height { get; }

        private readonly ITile[,] _tiles;

        public event Action<ITile> OnTileModified;

        /// <summary>
        /// Initializes a new instance of the TileGrid class.
        /// </summary>
        public TileGrid(int width, int height, TerrainType defaultTerrain = TerrainType.Ground)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Grid dimensions must be greater than zero.");

            Width = width;
            Height = height;
            _tiles = new ITile[width, height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var pos = new GridPosition(x, y);
                    var tile = new Tile(pos, defaultTerrain);
                    
                    _tiles[x, y] = tile;
                    tile.OnTerrainChanged += HandleTileTerrainChanged;
                }
            }
        }

        /// <summary>
        /// Retrieves the tile at the specified GridPosition, or null if out of bounds.
        /// </summary>
        public ITile GetTile(GridPosition position)
        {
            if (!IsValidPosition(position))
                return null;

            return _tiles[position.X, position.Y];
        }

        /// <summary>
        /// Determines if the specified GridPosition is within the grid boundaries.
        /// </summary>
        public bool IsValidPosition(GridPosition position)
        {
            return position.X >= 0 && position.X < Width &&
                   position.Y >= 0 && position.Y < Height;
        }

        /// <summary>
        /// Fired when an individual tile terrain changes, propagating the modification grid-wide.
        /// </summary>
        private void HandleTileTerrainChanged(ITile tile, TerrainType oldTerrain, TerrainType newTerrain)
        {
            OnTileModified?.Invoke(tile);
        }

        /// <summary>
        /// Cleans up event subscriptions to prevent memory leaks.
        /// </summary>
        public void Dispose()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (_tiles[x, y] != null)
                    {
                        _tiles[x, y].OnTerrainChanged -= HandleTileTerrainChanged;
                    }
                }
            }
        }
    }
}
