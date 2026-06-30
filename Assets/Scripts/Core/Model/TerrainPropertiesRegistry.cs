using System;
using System.Collections.Generic;
using Core.Commands;

namespace Core.Model
{
    public static class TerrainPropertiesRegistry
    {
        private static readonly Dictionary<TerrainType, ITerrainProperties> _registry = new Dictionary<TerrainType, ITerrainProperties>();

        /// <summary>
        /// Static constructor that registers terrain property definitions.
        /// </summary>
        static TerrainPropertiesRegistry()
        {
            Register(new TerrainProperties(
                TerrainType.Ground, 
                "Ground", 
                isObstacle: false, 
                movementCost: 1, 
                simpleEffect: "Nothing", 
                complexEffect: "Nothing"
            ));

            Register(new TerrainProperties(
                TerrainType.Grass, 
                "Grass", 
                isObstacle: false, 
                movementCost: 1, 
                simpleEffect: "Nothing", 
                complexEffect: "Restore HP at beginning of turn"
            ));

            Register(new TreeProperties());
            Register(new FireProperties());

            Register(new TerrainProperties(
                TerrainType.Rock, 
                "Rock", 
                isObstacle: true, 
                movementCost: 99, 
                simpleEffect: "Obstacle", 
                complexEffect: "Nothing"
            ));

            Register(new MudProperties());

            Register(new IceProperties());

            Register(new TerrainProperties(
                TerrainType.Water, 
                "Water", 
                isObstacle: false, 
                movementCost: 2, 
                simpleEffect: "2x Movespeed cost", 
                complexEffect: "Take less physical damage on stop"
            ));
        }

        /// <summary>
        /// Registers the properties of a terrain type.
        /// </summary>
        private static void Register(ITerrainProperties properties)
        {
            _registry[properties.Type] = properties;
        }

        /// <summary>
        /// Retrieves the static properties for a given terrain type.
        /// </summary>
        public static ITerrainProperties GetProperties(TerrainType type)
        {
            if (_registry.TryGetValue(type, out var properties))
            {
                return properties;
            }
            throw new ArgumentException($"No properties registered for terrain type: {type}");
        }

        private class TerrainProperties : ITerrainProperties
        {
            public TerrainType Type { get; }
            public string Name { get; }
            public bool IsObstacle { get; }
            public int MovementCost { get; }
            public string SimpleEffectDescription { get; }
            public string ComplexEffectDescription { get; }

            /// <summary>
            /// Initializes a new instance of the TerrainProperties class.
            /// </summary>
            public TerrainProperties(TerrainType type, string name, bool isObstacle, int movementCost, string simpleEffect, string complexEffect)
            {
                Type = type;
                Name = name;
                IsObstacle = isObstacle;
                MovementCost = movementCost;
                SimpleEffectDescription = simpleEffect;
                ComplexEffectDescription = complexEffect;
            }
        }

        private class TreeProperties : ITerrainProperties, IStopEffect
        {
            public TerrainType Type => TerrainType.Tree;
            public string Name => "Tree";
            public bool IsObstacle => false;
            public int MovementCost => 1;
            public string SimpleEffectDescription => "Evasion Up on stop (+15)";
            public string ComplexEffectDescription => "Take more magic damage";

            /// <summary>
            /// Executes a ModifyEvasionCommand to increase the character's evasion when stopping on a Tree tile.
            /// </summary>
            public void ApplyOnStop(ICharacter character, ITile tile, ICommandManager commandManager)
            {
                commandManager.ExecuteCommand(new ModifyEvasionCommand(character, 15));
            }
        }

        private class FireProperties : ITerrainProperties, ITraversalEffect, IStopEffect
        {
            public TerrainType Type => TerrainType.Fire;
            public string Name => "Fire";
            public bool IsObstacle => false;
            public int MovementCost => 1;
            public string SimpleEffectDescription => "Damage on traversal (-10 HP), apply Fire status";
            public string ComplexEffectDescription => "Deal additional magic damage on melee attack";

            /// <summary>
            /// Applies damage and status effects to the character when they traverse a Fire tile.
            /// </summary>
            public void ApplyOnTraversal(ICharacter character, ITile tile, ICommandManager commandManager)
            {
                commandManager.ExecuteCommand(new DamageCharacterCommand(character, 10));
                commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Fire"));
            }

            /// <summary>
            /// Applies the FireImbued status to the character when they stop on a Fire tile.
            /// </summary>
            public void ApplyOnStop(ICharacter character, ITile tile, ICommandManager commandManager)
            {
                commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "FireImbued"));
            }
        }

        private class MudProperties : ITerrainProperties, ITraversalEffect
        {
            public TerrainType Type => TerrainType.Mud;
            public string Name => "Mud";
            public bool IsObstacle => false;
            public int MovementCost => 2;
            public string SimpleEffectDescription => "2x Movespeed cost, apply Earth status on traversal";
            public string ComplexEffectDescription => "Take less magic damage on traversal";

            /// <summary>
            /// Applies status effects to the character when they traverse a Mud tile.
            /// </summary>
            public void ApplyOnTraversal(ICharacter character, ITile tile, ICommandManager commandManager)
            {
                commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Earth"));
                commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "MudTraversal"));
                commandManager.ExecuteCommand(new UpdateStatusDurationCommand(character, "MudTraversal", 1));
            }
        }

        private class IceProperties : ITerrainProperties, IStopEffect
        {
            public TerrainType Type => TerrainType.Ice;
            public string Name => "Ice";
            public bool IsObstacle => false;
            public int MovementCost => 1;
            public string SimpleEffectDescription => "- movespeed, can't turn";
            public string ComplexEffectDescription => "Evasion Down on stop";

            /// <summary>
            /// Applies the Frozen status and executes ModifyEvasionCommand to reduce the character's evasion when stopping on an Ice tile.
            /// </summary>
            public void ApplyOnStop(ICharacter character, ITile tile, ICommandManager commandManager)
            {
                commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Ice"));
                commandManager.ExecuteCommand(new ModifyEvasionCommand(character, -20));
            }
        }
    }
}
