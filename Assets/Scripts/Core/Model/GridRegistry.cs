namespace Core.Model
    {
        public static class GridRegistry
        {
            /// <summary>
            /// Global reference to the active tile grid model, used for terrain checks and path planning queries.
            /// </summary>
            public static ITileGrid Grid;
        }
    }
    
