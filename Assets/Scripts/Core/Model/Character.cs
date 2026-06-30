using System;
using System.Collections.Generic;

namespace Core.Model
{
    public class Character : ICharacter
    {
        public string Name { get; }
        public CharacterType CharacterType { get; }
        public Team Team { get; }
        public GridPosition Position { get; private set; }
        public int MaxHP { get; }
        public int CurrentHP { get; private set; }
        public int BaseMovementSpeed { get; }
        public float CurrentMovementSpeed { get; private set; }
        public int EvasionModifier { get; private set; }

        private readonly HashSet<string> _statuses = new HashSet<string>();

        public event Action<ICharacter> OnCharacterChanged;

        /// <summary>
        /// Initializes a new instance of the Character class.
        /// </summary>
        public Character(string name, GridPosition position, int maxHP, int baseMovementSpeed, Team team = Team.Player, CharacterType characterType = CharacterType.Warrior)
        {
            Name = name;
            Position = position;
            MaxHP = maxHP;
            CurrentHP = maxHP;
            BaseMovementSpeed = baseMovementSpeed;
            CurrentMovementSpeed = baseMovementSpeed;
            EvasionModifier = 0;
            Team = team;
            CharacterType = characterType;
        }

        /// <summary>
        /// Checks if the character currently has the specified status effect.
        /// </summary>
        public bool HasStatus(string statusName)
        {
            return _statuses.Contains(statusName);
        }

        /// <summary>
        /// Sets the character's position and triggers the OnCharacterChanged event.
        /// </summary>
        public void MoveTo(GridPosition newPosition)
        {
            Position = newPosition;
            OnCharacterChanged?.Invoke(this);
        }

        /// <summary>
        /// Subtracts the damage amount from the character's current HP and triggers the OnCharacterChanged event.
        /// </summary>
        public void ApplyDamage(int amount)
        {
            CurrentHP = Math.Max(0, CurrentHP - amount);
            OnCharacterChanged?.Invoke(this);
        }

        /// <summary>
        /// Restores HP to the character, capped at max HP, and triggers the OnCharacterChanged event.
        /// </summary>
        public void ApplyHeal(int amount)
        {
            CurrentHP = Math.Min(MaxHP, CurrentHP + amount);
            OnCharacterChanged?.Invoke(this);
        }

        /// <summary>
        /// Modifies the character's evasion modifier by the specified offset and triggers the OnCharacterChanged event.
        /// </summary>
        public void ModifyEvasion(int offset)
        {
            EvasionModifier += offset;
            OnCharacterChanged?.Invoke(this);
        }

        /// <summary>
        /// Modifies the character's current movement speed by the specified offset and triggers the OnCharacterChanged event.
        /// </summary>
        public void ModifyMovementSpeed(float offset)
        {
            CurrentMovementSpeed += offset;
            OnCharacterChanged?.Invoke(this);
        }

        /// <summary>
        /// Gets the collection of active status names on the character.
        /// </summary>
        public IReadOnlyCollection<string> Statuses => _statuses;

        private readonly Dictionary<string, int> _statusDurations = new Dictionary<string, int>();

        /// <summary>
        /// Retrieves the remaining turn duration for a specific status effect.
        /// </summary>
        public int GetStatusDuration(string statusName)
        {
            return _statusDurations.TryGetValue(statusName, out int duration) ? duration : 0;
        }

        /// <summary>
        /// Sets the remaining turn duration for a specific status effect.
        /// </summary>
        public void SetStatusDuration(string statusName, int duration)
        {
            _statusDurations[statusName] = duration;
        }

        /// <summary>
        /// Checks if the character currently carries any reaction status (Freeze, Blur, or speed/evasion reaction boosts).
        /// </summary>
        private bool HasReactionStatus()
        {
            return _statuses.Contains("Freeze") ||
                   _statuses.Contains("Blur") ||
                   _statuses.Contains("ExtinctionSpeedBoost") ||
                   _statuses.Contains("BurstEvasionBoost");
        }

        /// <summary>
        /// Adds a status effect to the character (with a default duration of 2 turns), unless blocked by a reaction status.
        /// </summary>
        public void AddStatus(string statusName)
        {
            if (HasReactionStatus())
            {
                return;
            }

            if (_statuses.Add(statusName))
            {
                _statusDurations[statusName] = 1; // Duration of 1 turn: expires at the end of the target's next active turn
                OnCharacterChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Removes a status effect from the character and triggers the OnCharacterChanged event.
        /// </summary>
        public void RemoveStatus(string statusName)
        {
            if (_statuses.Remove(statusName))
            {
                _statusDurations.Remove(statusName);
                OnCharacterChanged?.Invoke(this);
            }
        }
    }
}
