using System;
using System.Collections.Generic;

namespace Core.Model
{
    public interface ICharacter
    {
        string Name { get; }
        CharacterType CharacterType { get; }
        Team Team { get; }
        GridPosition Position { get; }
        int MaxHP { get; }
        int CurrentHP { get; }
        int BaseMovementSpeed { get; }
        float CurrentMovementSpeed { get; }
        int EvasionModifier { get; }
        
        /// <summary>
        /// Gets the collection of active status names on the character.
        /// </summary>
        IReadOnlyCollection<string> Statuses { get; }

        /// <summary>
        /// Retrieves the remaining turn duration for a specific status effect.
        /// </summary>
        int GetStatusDuration(string statusName);

        /// <summary>
        /// Sets the remaining turn duration for a specific status effect.
        /// </summary>
        void SetStatusDuration(string statusName, int duration);

        /// <summary>
        /// Checks if the character currently has a specific elemental or combat status.
        /// </summary>
        bool HasStatus(string statusName);

        /// <summary>
        /// Moves the character to a new GridPosition.
        /// </summary>
        void MoveTo(GridPosition newPosition);

        /// <summary>
        /// Applies damage to the character.
        /// </summary>
        void ApplyDamage(int amount);

        /// <summary>
        /// Restores HP to the character.
        /// </summary>
        void ApplyHeal(int amount);

        /// <summary>
        /// Modifies the character's evasion modifier.
        /// </summary>
        void ModifyEvasion(int offset);

        /// <summary>
        /// Modifies the character's current movement speed.
        /// </summary>
        void ModifyMovementSpeed(float offset);

        /// <summary>
        /// Adds a status effect to the character.
        /// </summary>
        void AddStatus(string statusName);

        /// <summary>
        /// Removes a status effect from the character.
        /// </summary>
        void RemoveStatus(string statusName);
        
        event Action<ICharacter> OnCharacterChanged;
    }
}
