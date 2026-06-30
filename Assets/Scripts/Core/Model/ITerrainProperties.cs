namespace Core.Model
{
    public interface ITerrainProperties
    {
        TerrainType Type { get; }
        string Name { get; }
        bool IsObstacle { get; }
        int MovementCost { get; }
        string SimpleEffectDescription { get; }
        string ComplexEffectDescription { get; }
    }

}
