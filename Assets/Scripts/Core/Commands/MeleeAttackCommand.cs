using System;
using Core.Model;

namespace Core.Commands
{
    public class MeleeAttackCommand : ICommand
    {
        private readonly ICharacter _attacker;
        private readonly ICharacter _target;
        private readonly ICommandManager _subCommandManager;

        /// <summary>
        /// Initializes a new instance of the MeleeAttackCommand class.
        /// </summary>
        public MeleeAttackCommand(ICharacter attacker, ICharacter target)
        {
            _attacker = attacker;
            _target = target;
            _subCommandManager = new CommandManager();
        }

        /// <summary>
        /// Validates adjacency and inflicts Physical and/or Magic damage to the target.
        /// </summary>
        public void Execute()
        {
            // Verify adjacency (distance of 1 in X and Y, including diagonals)
            int dx = Math.Abs(_attacker.Position.X - _target.Position.X);
            int dy = Math.Abs(_attacker.Position.Y - _target.Position.Y);

            if (dx > 1 || dy > 1 || (dx == 0 && dy == 0))
            {
                UnityEngine.Debug.LogWarning($"[MeleeAttackCommand] Target {_target.Name} is not adjacent to attacker {_attacker.Name}!");
                return;
            }

            // 1. Physical melee damage: 20 physical damage
            _subCommandManager.ExecuteCommand(new DamageCharacterCommand(_target, 20, DamageType.Physical));

            // 2. Fire Terrain complex effect: Deal +5 additional Magic damage on melee attack if FireImbued
            if (_attacker.HasStatus("FireImbued"))
            {
                _subCommandManager.ExecuteCommand(new DamageCharacterCommand(_target, 5, DamageType.Magic));
            }
        }

        /// <summary>
        /// Reverts the melee attack damage.
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
