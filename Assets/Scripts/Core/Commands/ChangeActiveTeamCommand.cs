using Core.Model;

namespace Core.Commands
{
    public class ChangeActiveTeamCommand : ICommand
    {
        private readonly Team _newTeam;
        private Team _previousTeam;

        public ChangeActiveTeamCommand(Team newTeam)
        {
            _newTeam = newTeam;
        }

        public void Execute()
        {
            _previousTeam = TurnRegistry.ActiveTeam;
            TurnRegistry.ActiveTeam = _newTeam;
        }

        public void Undo()
        {
            TurnRegistry.ActiveTeam = _previousTeam;
        }
    }
}
