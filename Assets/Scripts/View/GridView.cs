using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Core.Model;

namespace View
{
    public class GridView : MonoBehaviour
    {
        [Header("Unity Tilemap References")]
        [SerializeField] private Tilemap _tilemap;

        [Header("Terrain Tile Mapping")]
        [Tooltip("Assign your .asset tiles here to map them to the C# TerrainType enum")]
        [SerializeField] private List<TerrainTileMapEntry> _terrainMappings;

        public ITileGrid GridModel { get; private set; }

        private readonly Dictionary<TerrainType, TileBase> _typeToTileAsset = new Dictionary<TerrainType, TileBase>();
        private readonly Dictionary<TileBase, TerrainType> _tileAssetToType = new Dictionary<TileBase, TerrainType>();

        /// <summary>
        /// Unity Awake event. Initializes dictionaries and synchronizes the grid model.
        /// </summary>
        private void Awake()
        {
            InitializeMappingDictionaries();
            InitializeGridModelFromTilemap();
        }

        /// <summary>
        /// Builds fast-lookup dictionaries mapping TerrainType to TileBase and vice-versa.
        /// </summary>
        private void InitializeMappingDictionaries()
        {
            foreach (var entry in _terrainMappings)
            {
                if (entry.TileAsset == null) continue;
                
                _typeToTileAsset[entry.Type] = entry.TileAsset;
                _tileAssetToType[entry.TileAsset] = entry.Type;
            }
        }

        /// <summary>
        /// Generates the pure C# grid model from the layout painted in the Unity Tilemap component.
        /// </summary>
        private void InitializeGridModelFromTilemap()
        {
            if (_tilemap == null)
            {
                Debug.LogError("[GridView] Tilemap reference is missing!");
                return;
            }

            _tilemap.CompressBounds();
            BoundsInt bounds = _tilemap.cellBounds;
            
            int width = bounds.size.x;
            int height = bounds.size.y;
            
            if (width <= 0 || height <= 0)
            {
                Debug.LogWarning("[GridView] Painted tilemap is empty. Creating default 10x10 Grid.");
                width = 10;
                height = 10;
            }

            TileGrid concreteGrid = new TileGrid(width, height, TerrainType.Ground);
            GridModel = concreteGrid;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3Int tilemapCoord = new Vector3Int(x + bounds.xMin, y + bounds.yMin, 0);
                    TileBase paintedTile = _tilemap.GetTile(tilemapCoord);
                    TerrainType mappedType = TerrainType.Ground;

                    if (paintedTile != null && _tileAssetToType.TryGetValue(paintedTile, out TerrainType detectedType))
                    {
                        mappedType = detectedType;
                    }

                    ITile modelTile = concreteGrid.GetTile(new GridPosition(x, y));
                    if (modelTile != null)
                    {
                        modelTile.SetTerrain(mappedType);
                    }
                }
            }

            GridModel.OnTileModified += HandleGridTileModified;
            Debug.Log($"[GridView] Synchronized C# Grid Model ({width}x{height}) successfully with Unity Tilemap!");
        }

        /// <summary>
        /// Synchronizes changes in the core tile model back to the visual Unity Tilemap.
        /// </summary>
        private void HandleGridTileModified(ITile modelTile)
        {
            if (_tilemap == null) return;

            BoundsInt bounds = _tilemap.cellBounds;
            Vector3Int tilemapCoord = new Vector3Int(modelTile.Position.X + bounds.xMin, modelTile.Position.Y + bounds.yMin, 0);

            if (_typeToTileAsset.TryGetValue(modelTile.CurrentTerrain, out TileBase newTileAsset))
            {
                _tilemap.SetTile(tilemapCoord, newTileAsset);
            }
            else
            {
                _tilemap.SetTile(tilemapCoord, null);
            }
        }

        /// <summary>
        /// Highlights a specific tile with a custom color.
        /// </summary>
        public void HighlightTile(GridPosition pos, Color color)
        {
            if (_tilemap == null) return;
            _tilemap.CompressBounds();
            BoundsInt bounds = _tilemap.cellBounds;
            Vector3Int tilemapCoord = new Vector3Int(pos.X + bounds.xMin, pos.Y + bounds.yMin, 0);
            if (_tilemap.HasTile(tilemapCoord))
            {
                _tilemap.SetTileFlags(tilemapCoord, TileFlags.None);
                _tilemap.SetColor(tilemapCoord, color);
            }
        }

        /// <summary>
        /// Clears all custom tile highlights by resetting their color to white.
        /// </summary>
        public void ClearHighlights()
        {
            if (_tilemap == null) return;
            _tilemap.CompressBounds();
            BoundsInt bounds = _tilemap.cellBounds;
            int width = GridModel != null ? GridModel.Width : bounds.size.x;
            int height = GridModel != null ? GridModel.Height : bounds.size.y;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3Int tilemapCoord = new Vector3Int(x + bounds.xMin, y + bounds.yMin, 0);
                    if (_tilemap.HasTile(tilemapCoord))
                    {
                        _tilemap.SetTileFlags(tilemapCoord, TileFlags.None);
                        _tilemap.SetColor(tilemapCoord, Color.white);
                    }
                }
            }
        }

        /// <summary>
        /// Unsubscribes from events when the GridView is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (GridModel != null)
            {
                GridModel.OnTileModified -= HandleGridTileModified;
            }
        }

        [System.Serializable]
        public struct TerrainTileMapEntry
        {
            public TerrainType Type;
            public TileBase TileAsset;
        }
    }
}
