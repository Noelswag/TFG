using System.Collections.Generic;
using Core.Model;

namespace Core.Commands
{
    public class ApplyStatusEffectCommand : ICommand
    {
        private readonly ICharacter _character;
        private readonly string _statusName;
        private readonly ITileGrid _grid;
        private readonly IEnumerable<ICharacter> _allCharacters;
        private bool _didApply;
        private ICommandManager _reactionSubCommandManager;

        /// <summary>
        /// Initializes a new instance of the ApplyStatusEffectCommand class.
        /// </summary>
        public ApplyStatusEffectCommand(ICharacter character, string statusName, ITileGrid grid = null, IEnumerable<ICharacter> allCharacters = null)
        {
            _character = character;
            _statusName = statusName;
            _grid = grid;
            _allCharacters = allCharacters;
        }

        /// <summary>
        /// Applies the status effect to the character, checking and resolving elemental reactions first.
        /// </summary>
        public void Execute()
        {
            _reactionSubCommandManager = new CommandManager();

            if (CheckAndTriggerReactions())
            {
                _didApply = false;
                return;
            }

            if (!_character.HasStatus(_statusName))
            {
                _character.AddStatus(_statusName);
                _didApply = _character.HasStatus(_statusName);

                if (_didApply)
                {
                    // Handle status-specific stat modifications
                    if (_statusName == "Freeze")
                    {
                        _reactionSubCommandManager.ExecuteCommand(new ModifyMovementSpeedCommand(_character, -_character.CurrentMovementSpeed));
                    }
                    else if (_statusName == "Blur")
                    {
                        _reactionSubCommandManager.ExecuteCommand(new ModifyMovementSpeedCommand(_character, 2));
                    }
                    else if (_statusName == "ExtinctionSpeedBoost")
                    {
                        _reactionSubCommandManager.ExecuteCommand(new ModifyMovementSpeedCommand(_character, 2));
                    }
                    else if (_statusName == "BurstEvasionBoost")
                    {
                        _reactionSubCommandManager.ExecuteCommand(new ModifyEvasionCommand(_character, 15));
                    }
                }
            }
        }

        /// <summary>
        /// Reverts the status application and any associated reaction commands.
        /// </summary>
        public void Undo()
        {
            if (_reactionSubCommandManager != null)
            {
                while (_reactionSubCommandManager.CanUndo)
                {
                    _reactionSubCommandManager.Undo();
                }
            }

            if (_didApply)
            {
                _character.RemoveStatus(_statusName);
            }
        }

        /// <summary>
        /// Checks if the new status triggers a reaction with existing statuses on the character.
        /// </summary>
        private bool CheckAndTriggerReactions()
        {
            var allChars = _allCharacters ?? CharacterRegistry.AllCharacters;
            var activeGrid = _grid ?? GridRegistry.Grid;

            // 1. Ice + Wind = Nothing
            if ((_statusName == "Ice" && _character.HasStatus("Wind")) ||
                (_statusName == "Wind" && _character.HasStatus("Ice")))
            {
                _reactionSubCommandManager.ExecuteCommand(new RemoveStatusEffectCommand(_character, "Ice"));
                _reactionSubCommandManager.ExecuteCommand(new RemoveStatusEffectCommand(_character, "Wind"));
                return true;
            }

            // 2. Earth + Fire = Nothing
            if ((_statusName == "Earth" && _character.HasStatus("Fire")) ||
                (_statusName == "Fire" && _character.HasStatus("Earth")))
            {
                _reactionSubCommandManager.ExecuteCommand(new RemoveStatusEffectCommand(_character, "Earth"));
                _reactionSubCommandManager.ExecuteCommand(new RemoveStatusEffectCommand(_character, "Fire"));
                return true;
            }

            // 3. Ice + Fire = Extinction
            if ((_statusName == "Ice" && _character.HasStatus("Fire")) ||
                (_statusName == "Fire" && _character.HasStatus("Ice")))
            {
                _reactionSubCommandManager.ExecuteCommand(new RemoveStatusEffectCommand(_character, "Ice"));
                _reactionSubCommandManager.ExecuteCommand(new RemoveStatusEffectCommand(_character, "Fire"));
                _reactionSubCommandManager.ExecuteCommand(new DamageCharacterCommand(_character, 20));
                _reactionSubCommandManager.ExecuteCommand(new ApplyStatusEffectCommand(_character, "ExtinctionSpeedBoost", activeGrid, allChars));
                return true;
            }

            // 4. Ice + Earth = Freeze
            if ((_statusName == "Ice" && _character.HasStatus("Earth")) ||
                (_statusName == "Earth" && _character.HasStatus("Ice")))
            {
                _reactionSubCommandManager.ExecuteCommand(new RemoveStatusEffectCommand(_character, "Ice"));
                _reactionSubCommandManager.ExecuteCommand(new RemoveStatusEffectCommand(_character, "Earth"));
                _reactionSubCommandManager.ExecuteCommand(new ApplyStatusEffectCommand(_character, "Freeze", activeGrid, allChars));
                return true;
            }

            // 5. Wind + Fire = Burst
            if ((_statusName == "Wind" && _character.HasStatus("Fire")) ||
                (_statusName == "Fire" && _character.HasStatus("Wind")))
            {
                _reactionSubCommandManager.ExecuteCommand(new RemoveStatusEffectCommand(_character, "Wind"));
                _reactionSubCommandManager.ExecuteCommand(new RemoveStatusEffectCommand(_character, "Fire"));
                _reactionSubCommandManager.ExecuteCommand(new DamageCharacterCommand(_character, 15));
                _reactionSubCommandManager.ExecuteCommand(new ApplyStatusEffectCommand(_character, "BurstEvasionBoost", activeGrid, allChars));

                // AOE damage to adjacent tiles
                if (activeGrid != null)
                {
                    HashSet<GridPosition> adjacent = new HashSet<GridPosition>(_character.Position.GetNeighbors());
                    foreach (var other in allChars)
                    {
                        if (other != _character && adjacent.Contains(other.Position))
                        {
                            _reactionSubCommandManager.ExecuteCommand(new DamageCharacterCommand(other, 10));
                        }
                    }
                }
                return true;
            }

            // 6. Wind + Earth = Blur
            if ((_statusName == "Wind" && _character.HasStatus("Earth")) ||
                (_statusName == "Earth" && _character.HasStatus("Wind")))
            {
                _reactionSubCommandManager.ExecuteCommand(new RemoveStatusEffectCommand(_character, "Wind"));
                _reactionSubCommandManager.ExecuteCommand(new RemoveStatusEffectCommand(_character, "Earth"));
                _reactionSubCommandManager.ExecuteCommand(new ApplyStatusEffectCommand(_character, "Blur", activeGrid, allChars));
                return true;
            }

            return false;
        }
    }
}