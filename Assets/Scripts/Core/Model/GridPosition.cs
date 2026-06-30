using System;

namespace Core.Model
{
    public struct GridPosition : IEquatable<GridPosition>
    {
        public int X { get; }
        public int Y { get; }

        /// <summary>
        /// Initializes a new instance of the GridPosition struct with x and y coordinates.
        /// </summary>
        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Evaluates if this GridPosition has the same coordinates as another GridPosition.
        /// </summary>
        public bool Equals(GridPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        /// <summary>
        /// Evaluates if this GridPosition is equal to the specified object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        /// <summary>
        /// Generates a hash code for the GridPosition.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        /// <summary>
        /// Obtains the 4 cardinal neighbor GridPositions (North, South, East, West).
        /// </summary>
        public GridPosition[] GetNeighbors()
        {
            return new GridPosition[]
            {
                new GridPosition(X, Y + 1),
                new GridPosition(X, Y - 1),
                new GridPosition(X - 1, Y),
                new GridPosition(X + 1, Y)
            };
        }

        /// <summary>
        /// Calculates the Manhattan distance from this GridPosition to another GridPosition.
        /// </summary>
        public int DistanceTo(GridPosition other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        /// <summary>
        /// Compares two GridPosition instances for equality.
        /// </summary>
        public static bool operator ==(GridPosition left, GridPosition right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two GridPosition instances for inequality.
        /// </summary>
        public static bool operator !=(GridPosition left, GridPosition right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Adds the coordinates of two GridPositions.
        /// </summary>
        public static GridPosition operator +(GridPosition a, GridPosition b)
        {
            return new GridPosition(a.X + b.X, a.Y + b.Y);
        }

        /// <summary>
        /// Subtracts the coordinates of one GridPosition from another.
        /// </summary>
        public static GridPosition operator -(GridPosition a, GridPosition b)
        {
            return new GridPosition(a.X - b.X, a.Y - b.Y);
        }

        /// <summary>
        /// Returns a formatted string representing the coordinates of the GridPosition.
        /// </summary>
        public override string ToString() => $"({X}, {Y})";
    }
}
