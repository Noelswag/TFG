using System.Collections.Generic;
using Core.Model;

namespace Core.Commands
{
    public class TurnTransitionCommand : ICommand
    {
        private readonly IEnumerable<ICharacter> _allCharacters;
        private readonly ITileGrid _grid;
        private readonly ICommandManager _mainCommandManager;
        private readonly bool _lockHistory;
        private readonly ICommandManager _subCommandManager;

        /// <summary>
        /// Initializes a new instance of the TurnTransitionCommand class.
        /// </summary>
        public TurnTransitionCommand(
            IEnumerable<ICharacter> allCharacters, 
            ITileGrid grid, 
            ICommandManager mainCommandManager = null, 
            bool lockHistory = true)
        {
            _allCharacters = allCharacters;
            _grid = grid;
            _mainCommandManager = mainCommandManager;
            _lockHistory = lockHistory;
            _subCommandManager = new CommandManager();
        }

        /// <summary>
        /// Transition turns by ending the current turn and starting the next one.
        /// If history locking is enabled, clears the main command history.
        /// </summary>
        public void Execute()
        {
            // Check if there are multiple teams in play
            bool hasMultipleTeams = false;
            Team firstTeam = Team.Player;
            bool firstSet = false;
            foreach (var c in _allCharacters)
            {
                if (!firstSet)
                {
                    firstTeam = c.Team;
                    firstSet = true;
                }
                else if (c.Team != firstTeam)
                {
                    hasMultipleTeams = true;
                    break;
                }
            }

            if (hasMultipleTeams)
            {
                Team currentTeam = TurnRegistry.ActiveTeam;
                Team nextTeam = currentTeam == Team.Player ? Team.Enemy : Team.Player;

                // 1. End current turn (status ticks for ending team)
                _subCommandManager.ExecuteCommand(new EndTurnCommand(_allCharacters, _grid, currentTeam));
                
                // 2. Toggle active team
                _subCommandManager.ExecuteCommand(new ChangeActiveTeamCommand(nextTeam));

                // 3. Start next turn (healing and speed for starting team)
                _subCommandManager.ExecuteCommand(new StartTurnCommand(_allCharacters, _grid, nextTeam));
            }
            else
            {
                // Fallback for single-team scenarios (e.g. legacy unit tests)
                _subCommandManager.ExecuteCommand(new EndTurnCommand(_allCharacters, _grid, null));
                _subCommandManager.ExecuteCommand(new StartTurnCommand(_allCharacters, _grid, null));
            }

            // 4. Lock in all decisions by clearing the main command manager's history
            if (_lockHistory && _mainCommandManager != null)
            {
                _mainCommandManager.ClearHistory();

                System.Action clearHandler = null;
                clearHandler = () =>
                {
                    _mainCommandManager.OnHistoryChanged -= clearHandler;
                    _mainCommandManager.ClearHistory();
                };
                _mainCommandManager.OnHistoryChanged += clearHandler;
            }
        }

        /// <summary>
        /// Undoes the turn transition, rolling back both start and end turn effects.
        /// </summary>
        public void Undo()
        {
            while (_subCommandManager.CanUndo)
            {
                _subCommandManager.Undo();
            }
        }
    }
}
