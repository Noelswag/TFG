using System.Collections.Generic;

namespace Core.Model
{
    public interface IPathfinder
    {
        /// <summary>
        /// Calculates the shortest valid path from start to end within the movement budget.
        /// </summary>
        List<GridPosition> FindPath(ITileGrid grid, GridPosition start, GridPosition end, int movementBudget);

        /// <summary>
        /// Returns all coordinates reachable from a start position within a movement budget.
        /// </summary>
        HashSet<GridPosition> GetReachableTiles(ITileGrid grid, GridPosition start, int movementBudget);
    }
}
