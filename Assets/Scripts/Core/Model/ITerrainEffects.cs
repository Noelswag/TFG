using Core.Commands;

namespace Core.Model
{
    public interface IStopEffect
    {
        /// <summary>
        /// Applies an effect when a character stops on this tile.
        /// </summary>
        void ApplyOnStop(ICharacter character, ITile tile, ICommandManager commandManager);
    }

    public interface ITraversalEffect
    {
        /// <summary>
        /// Applies an effect when a character traverses this tile.
        /// </summary>
        void ApplyOnTraversal(ICharacter character, ITile tile, ICommandManager commandManager);
    }
}
