using System;
using System.Collections.Generic;

namespace Core.Model
{
    public class DijkstraPathfinder : IPathfinder
    {
        /// <summary>
        /// Finds the shortest path between start and end GridPositions using Dijkstra's algorithm, limited by the movement budget.
        /// </summary>
        public List<GridPosition> FindPath(ITileGrid grid, GridPosition start, GridPosition end, int movementBudget)
        {
            if (!grid.IsValidPosition(start) || !grid.IsValidPosition(end))
                return null;

            if (start == end)
                return new List<GridPosition>();

            var distances = new Dictionary<GridPosition, int>();
            var previous = new Dictionary<GridPosition, GridPosition>();
            var openSet = new List<GridPosition>();
            var closedSet = new HashSet<GridPosition>();

            distances[start] = 0;
            openSet.Add(start);

            while (openSet.Count > 0)
            {
                openSet.Sort((a, b) => distances[a].CompareTo(distances[b]));
                GridPosition current = openSet[0];
                openSet.RemoveAt(0);

                if (current == end)
                    break;

                closedSet.Add(current);

                bool isIce = false;
                GridPosition forcedNeighbor = new GridPosition();
                ITile currentTile = grid.GetTile(current);
                
                if (currentTile != null && currentTile.CurrentTerrain == TerrainType.Ice)
                {
                    if (previous.TryGetValue(current, out GridPosition parent))
                    {
                        GridPosition entryDir = current - parent;
                        forcedNeighbor = current + entryDir;
                        isIce = true;
                    }
                }

                foreach (GridPosition neighbor in current.GetNeighbors())
                {
                    if (isIce && neighbor != forcedNeighbor)
                        continue;

                    if (!grid.IsValidPosition(neighbor) || closedSet.Contains(neighbor))
                        continue;

                    ITile tile = grid.GetTile(neighbor);
                    if (tile == null || tile.Properties.IsObstacle)
                        continue;

                    int stepCost = tile.Properties.MovementCost;
                    int tentativeCost = distances[current] + stepCost;

                    if (tentativeCost > movementBudget)
                        continue;

                    if (!distances.ContainsKey(neighbor) || tentativeCost < distances[neighbor])
                    {
                        distances[neighbor] = tentativeCost;
                        previous[neighbor] = current;

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            if (!distances.ContainsKey(end) || distances[end] > movementBudget)
                return null;

            var path = new List<GridPosition>();
            GridPosition curr = end;
            while (curr != start)
            {
                path.Add(curr);
                curr = previous[curr];
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Calculates the set of all GridPositions that can be reached from the start position within the movement budget.
        /// </summary>
        public HashSet<GridPosition> GetReachableTiles(ITileGrid grid, GridPosition start, int movementBudget)
        {
            var reachable = new HashSet<GridPosition>();
            if (!grid.IsValidPosition(start))
                return reachable;

            var distances = new Dictionary<GridPosition, int>();
            var previous = new Dictionary<GridPosition, GridPosition>();
            var openSet = new List<GridPosition>();
            var closedSet = new HashSet<GridPosition>();

            distances[start] = 0;
            openSet.Add(start);

            while (openSet.Count > 0)
            {
                openSet.Sort((a, b) => distances[a].CompareTo(distances[b]));
                GridPosition current = openSet[0];
                openSet.RemoveAt(0);

                closedSet.Add(current);
                reachable.Add(current);

                bool isIce = false;
                GridPosition forcedNeighbor = new GridPosition();
                ITile currentTile = grid.GetTile(current);
                
                if (currentTile != null && currentTile.CurrentTerrain == TerrainType.Ice)
                {
                    if (previous.TryGetValue(current, out GridPosition parent))
                    {
                        GridPosition entryDir = current - parent;
                        forcedNeighbor = current + entryDir;
                        isIce = true;
                    }
                }

                foreach (GridPosition neighbor in current.GetNeighbors())
                {
                    if (isIce && neighbor != forcedNeighbor)
                        continue;

                    if (!grid.IsValidPosition(neighbor) || closedSet.Contains(neighbor))
                        continue;

                    ITile tile = grid.GetTile(neighbor);
                    if (tile == null || tile.Properties.IsObstacle)
                        continue;

                    int stepCost = tile.Properties.MovementCost;
                    int tentativeCost = distances[current] + stepCost;

                    if (tentativeCost > movementBudget)
                        continue;

                    if (!distances.ContainsKey(neighbor) || tentativeCost < distances[neighbor])
                    {
                        distances[neighbor] = tentativeCost;
                        previous[neighbor] = current;

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            return reachable;
        }
    }
}
