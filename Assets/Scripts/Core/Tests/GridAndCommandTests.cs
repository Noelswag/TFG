using NUnit.Framework;
using System.Collections.Generic;
using Core.Commands;
using Core.Model;

namespace Core.Tests
{
    [TestFixture]
    public class GridAndCommandTests
    {
        private TileGrid _grid;
        private CommandManager _commandManager;

        /// <summary>
        /// Sets up a fresh 5x5 grid and command manager before each test.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _grid = new TileGrid(5, 5, TerrainType.Ground);
            _commandManager = new CommandManager();
        }

        /// <summary>
        /// Cleans up grid event subscriptions after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _grid.Dispose();
        }

        /// <summary>
        /// Verifies that grid dimensions and default terrain tiles are initialized correctly.
        /// </summary>
        [Test]
        public void When_GridInitializedWith5x5Ground_Expect_Width5Height5AndGroundTiles()
        {
            Assert.AreEqual(5, _grid.Width);
            Assert.AreEqual(5, _grid.Height);

            ITile centerTile = _grid.GetTile(new GridPosition(2, 2));
            Assert.IsNotNull(centerTile);
            Assert.AreEqual(TerrainType.Ground, centerTile.CurrentTerrain);
            Assert.AreEqual(1, centerTile.Properties.MovementCost);
            Assert.IsFalse(centerTile.Properties.IsObstacle);
        }

        /// <summary>
        /// Verifies that the IsValidPosition method correctly bounds-checks coordinates.
        /// </summary>
        [Test]
        public void When_CheckingGridBoundsOf5x5_Expect_0x0And4x4InsideAnd5x5Outside()
        {
            Assert.IsTrue(_grid.IsValidPosition(new GridPosition(0, 0)));
            Assert.IsTrue(_grid.IsValidPosition(new GridPosition(4, 4)));

            Assert.IsFalse(_grid.IsValidPosition(new GridPosition(-1, 2)));
            Assert.IsFalse(_grid.IsValidPosition(new GridPosition(2, 5)));
            Assert.IsFalse(_grid.IsValidPosition(new GridPosition(5, 5)));
        }

        /// <summary>
        /// Verifies that Mud terrain attributes match expected cost and obstacle settings.
        /// </summary>
        [Test]
        public void When_QueryingMudProperties_Expect_MovementCost2AndNotObstacle()
        {
            ITerrainProperties mud = TerrainPropertiesRegistry.GetProperties(TerrainType.Mud);
            
            Assert.AreEqual(2, mud.MovementCost);
            Assert.IsFalse(mud.IsObstacle);
            Assert.AreEqual("Mud", mud.Name);
        }

        /// <summary>
        /// Verifies that ChangeTerrainCommand correctly executes and undoes terrain changes.
        /// </summary>
        [Test]
        public void When_ChangeTerrainFromGroundToWaterAndUndone_Expect_TerrainStateUpdatesToWaterAndRevertsToGround()
        {
            ITile tile = _grid.GetTile(new GridPosition(1, 1));
            Assert.AreEqual(TerrainType.Ground, tile.CurrentTerrain);

            var command = new ChangeTerrainCommand(tile, TerrainType.Water);
            
            command.Execute();
            Assert.AreEqual(TerrainType.Water, tile.CurrentTerrain);
            Assert.AreEqual(2, tile.Properties.MovementCost);

            command.Undo();
            Assert.AreEqual(TerrainType.Ground, tile.CurrentTerrain);
            Assert.AreEqual(1, tile.Properties.MovementCost);
        }

        /// <summary>
        /// Verifies that executing multiple terrain changes and undoing/redoing them updates state properly.
        /// </summary>
        [Test]
        public void When_ExecutingGroundToGrassThenToMudAndUndoing_Expect_RevertsToGrassThenGroundAndRedoesToGrass()
        {
            ITile tile = _grid.GetTile(new GridPosition(0, 0));
            
            var toGrass = new ChangeTerrainCommand(tile, TerrainType.Grass);
            var toMud = new ChangeTerrainCommand(tile, TerrainType.Mud);

            _commandManager.ExecuteCommand(toGrass);
            _commandManager.ExecuteCommand(toMud);

            Assert.AreEqual(TerrainType.Mud, tile.CurrentTerrain);
            Assert.IsTrue(_commandManager.CanUndo);
            Assert.IsFalse(_commandManager.CanRedo);

            _commandManager.Undo();
            Assert.AreEqual(TerrainType.Grass, tile.CurrentTerrain);
            Assert.IsTrue(_commandManager.CanUndo);
            Assert.IsTrue(_commandManager.CanRedo);

            _commandManager.Undo();
            Assert.AreEqual(TerrainType.Ground, tile.CurrentTerrain);
            Assert.IsFalse(_commandManager.CanUndo);

            _commandManager.Redo();
            Assert.AreEqual(TerrainType.Grass, tile.CurrentTerrain);
            Assert.IsTrue(_commandManager.CanUndo);
        }

        /// <summary>
        /// Verifies that clearing command history locks in terrain changes and disables undo.
        /// </summary>
        [Test]
        public void When_ChangeToRockCommandHistoryCleared_Expect_TileRemainsRockAndCannotUndo()
        {
            ITile tile = _grid.GetTile(new GridPosition(0, 0));
            var toRock = new ChangeTerrainCommand(tile, TerrainType.Rock);

            _commandManager.ExecuteCommand(toRock);
            Assert.AreEqual(TerrainType.Rock, tile.CurrentTerrain);
            Assert.IsTrue(_commandManager.CanUndo);

            _commandManager.ClearHistory();

            Assert.AreEqual(TerrainType.Rock, tile.CurrentTerrain);
            Assert.IsFalse(_commandManager.CanUndo);
        }

        /// <summary>
        /// Verifies that DijkstraPathfinder routes paths around Rock obstacles correctly.
        /// </summary>
        [Test]
        public void When_PathfinderFindsShortestPathAroundRockObstacle_Expect_RockAvoidedAndPathCorrect()
        {
            ITile rockTile = _grid.GetTile(new GridPosition(0, 1));
            rockTile.SetTerrain(TerrainType.Rock);

            var pathfinder = new DijkstraPathfinder();
            List<GridPosition> path = pathfinder.FindPath(_grid, new GridPosition(0, 0), new GridPosition(0, 2), 5);

            Assert.IsNotNull(path);
            Assert.IsFalse(path.Contains(new GridPosition(0, 1)));
            Assert.IsTrue(path.Count > 2);
            Assert.AreEqual(new GridPosition(0, 2), path[path.Count - 1]);
        }

        /// <summary>
        /// Verifies that DijkstraPathfinder returns null when path cost exceeds budget.
        /// </summary>
        [Test]
        public void When_PathCostExceedsBudget_Expect_PathfinderReturnsNull()
        {
            _grid.GetTile(new GridPosition(0, 1)).SetTerrain(TerrainType.Mud);
            _grid.GetTile(new GridPosition(0, 2)).SetTerrain(TerrainType.Mud);
            _grid.GetTile(new GridPosition(0, 3)).SetTerrain(TerrainType.Mud);

            var pathfinder = new DijkstraPathfinder();
            List<GridPosition> path = pathfinder.FindPath(_grid, new GridPosition(0, 0), new GridPosition(0, 3), 5);
            
            Assert.IsNull(path);
        }

        /// <summary>
        /// Verifies that traversing Fire applies damage/status and stopping on Tree buffs evasion, and undoing reverts everything.
        /// </summary>
        [Test]
        public void When_CharacterMovesThroughFireAndStopsOnTree_Expect_EvasionUpAndDamageAppliedAndUndoRestoresEverything()
        {
            var pathfinder = new DijkstraPathfinder();
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);

            _grid.GetTile(new GridPosition(0, 1)).SetTerrain(TerrainType.Fire);
            _grid.GetTile(new GridPosition(0, 2)).SetTerrain(TerrainType.Tree);

            List<GridPosition> path = pathfinder.FindPath(_grid, new GridPosition(0, 0), new GridPosition(0, 2), 5);
            Assert.IsNotNull(path);

            var moveCommand = new MoveCharacterCommand(character, new GridPosition(0, 2), path, _grid);
            _commandManager.ExecuteCommand(moveCommand);

            Assert.AreEqual(new GridPosition(0, 2), character.Position);
            Assert.AreEqual(90, character.CurrentHP);
            Assert.IsTrue(character.HasStatus("Fire"));
            Assert.AreEqual(15, character.EvasionModifier);

            _commandManager.Undo();

            Assert.AreEqual(new GridPosition(0, 0), character.Position);
            Assert.AreEqual(100, character.CurrentHP);
            Assert.IsFalse(character.HasStatus("Fire"));
            Assert.AreEqual(0, character.EvasionModifier);
        }

        /// <summary>
        /// Verifies that traversing Mud applies Earth status and undoing removes it.
        /// </summary>
        [Test]
        public void When_CharacterMovesThroughMud_Expect_EarthStatusAppliedAndUndoRemovesIt()
        {
            var pathfinder = new DijkstraPathfinder();
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);

            _grid.GetTile(new GridPosition(0, 1)).SetTerrain(TerrainType.Mud);

            List<GridPosition> path = pathfinder.FindPath(_grid, new GridPosition(0, 0), new GridPosition(0, 1), 5);
            
            var moveCommand = new MoveCharacterCommand(character, new GridPosition(0, 1), path, _grid);
            _commandManager.ExecuteCommand(moveCommand);

            Assert.IsTrue(character.HasStatus("Earth"));

            _commandManager.Undo();

            Assert.IsFalse(character.HasStatus("Earth"));
        }

        /// <summary>
        /// Verifies that character slides in a straight line on Ice tiles and cannot turn until exit.
        /// </summary>
        [Test]
        public void When_PathfinderEncountersIceTile_Expect_CharacterSlidesInStraightLineAndCannotTurn()
        {
            var pathfinder = new DijkstraPathfinder();

            _grid.GetTile(new GridPosition(0, 1)).SetTerrain(TerrainType.Ice);
            _grid.GetTile(new GridPosition(1, 0)).SetTerrain(TerrainType.Rock);

            List<GridPosition> path = pathfinder.FindPath(_grid, new GridPosition(0, 0), new GridPosition(1, 1), 6);

            Assert.IsNotNull(path);
            
            Assert.AreEqual(4, path.Count);
            Assert.AreEqual(new GridPosition(0, 1), path[0]);
            Assert.AreEqual(new GridPosition(0, 2), path[1]);
            Assert.AreEqual(new GridPosition(1, 2), path[2]);
            Assert.AreEqual(new GridPosition(1, 1), path[3]);
        }

        /// <summary>
        /// Verifies that Ice and Wind statuses cleanse each other, leaving the character clean.
        /// </summary>
        [Test]
        public void When_IceAndWindApplied_Expect_CleansesBothAndNoOtherEffect()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Ice"));
            Assert.IsTrue(character.HasStatus("Ice"));

            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Wind"));
            Assert.IsFalse(character.HasStatus("Ice"));
            Assert.IsFalse(character.HasStatus("Wind"));
            Assert.AreEqual(100, character.CurrentHP);

            _commandManager.Undo();
            Assert.IsTrue(character.HasStatus("Ice"));
            Assert.IsFalse(character.HasStatus("Wind"));
        }

        /// <summary>
        /// Verifies that Earth and Fire statuses cleanse each other.
        /// </summary>
        [Test]
        public void When_EarthAndFireApplied_Expect_CleansesBothAndNoOtherEffect()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Earth"));
            Assert.IsTrue(character.HasStatus("Earth"));

            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Fire"));
            Assert.IsFalse(character.HasStatus("Earth"));
            Assert.IsFalse(character.HasStatus("Fire"));

            _commandManager.Undo();
            Assert.IsTrue(character.HasStatus("Earth"));
            Assert.IsFalse(character.HasStatus("Fire"));
        }

        /// <summary>
        /// Verifies that Ice and Fire result in Extinction, dealing 20 damage and giving speed boost +2, reverting on Undo.
        /// </summary>
        [Test]
        public void When_IceAndFireApplied_Expect_ExtinctionDamageAndSpeedBoostAndRollbackOnUndo()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Ice"));
            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Fire"));

            Assert.IsFalse(character.HasStatus("Ice"));
            Assert.IsFalse(character.HasStatus("Fire"));
            Assert.IsTrue(character.HasStatus("ExtinctionSpeedBoost"));
            Assert.AreEqual(80, character.CurrentHP);
            Assert.AreEqual(7, character.CurrentMovementSpeed);

            _commandManager.Undo();
            Assert.IsTrue(character.HasStatus("Ice"));
            Assert.IsFalse(character.HasStatus("Fire"));
            Assert.IsFalse(character.HasStatus("ExtinctionSpeedBoost"));
            Assert.AreEqual(100, character.CurrentHP);
            Assert.AreEqual(5, character.CurrentMovementSpeed);
        }

        /// <summary>
        /// Verifies that Ice and Earth trigger Freeze, reducing movement speed to 0 and halving incoming damage.
        /// </summary>
        [Test]
        public void When_IceAndEarthApplied_Expect_FreezeMovementSpeedZeroAndDamageHalvedAndRollbackOnUndo()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Ice"));
            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Earth"));

            Assert.IsFalse(character.HasStatus("Ice"));
            Assert.IsFalse(character.HasStatus("Earth"));
            Assert.IsTrue(character.HasStatus("Freeze"));
            Assert.AreEqual(0, character.CurrentMovementSpeed);

            // Test damage reduction (Physical damage is halved under Freeze)
            _commandManager.ExecuteCommand(new DamageCharacterCommand(character, 20, DamageType.Physical));
            Assert.AreEqual(90, character.CurrentHP); // 20 damage halved to 10

            _commandManager.Undo(); // Undo damage
            Assert.AreEqual(100, character.CurrentHP);

            _commandManager.Undo(); // Undo Freeze
            Assert.IsTrue(character.HasStatus("Ice"));
            Assert.IsFalse(character.HasStatus("Earth"));
            Assert.IsFalse(character.HasStatus("Freeze"));
            Assert.AreEqual(5, character.CurrentMovementSpeed);
        }

        /// <summary>
        /// Verifies that Wind and Fire trigger Burst, dealing AOE damage and granting a temporary evasion buff.
        /// </summary>
        [Test]
        public void When_WindAndFireApplied_Expect_BurstAoeDamageAndEvasionBoostAndRollbackOnUndo()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            var adjacentChar = new Character("Ally", new GridPosition(0, 1), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character, adjacentChar };
            GridRegistry.Grid = _grid;

            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Wind"));
            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Fire"));

            Assert.IsFalse(character.HasStatus("Wind"));
            Assert.IsFalse(character.HasStatus("Fire"));
            Assert.IsTrue(character.HasStatus("BurstEvasionBoost"));
            Assert.AreEqual(85, character.CurrentHP); // 15 damage
            Assert.AreEqual(90, adjacentChar.CurrentHP); // 10 AOE damage
            Assert.AreEqual(15, character.EvasionModifier);

            _commandManager.Undo();
            Assert.IsTrue(character.HasStatus("Wind"));
            Assert.IsFalse(character.HasStatus("Fire"));
            Assert.IsFalse(character.HasStatus("BurstEvasionBoost"));
            Assert.AreEqual(100, character.CurrentHP);
            Assert.AreEqual(100, adjacentChar.CurrentHP);
            Assert.AreEqual(0, character.EvasionModifier);
        }

        /// <summary>
        /// Verifies that Wind and Earth trigger Blur, increasing speed by 2.
        /// </summary>
        [Test]
        public void When_WindAndEarthApplied_Expect_BlurSpeedIncreaseAndRollbackOnUndo()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Wind"));
            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Earth"));

            Assert.IsFalse(character.HasStatus("Wind"));
            Assert.IsFalse(character.HasStatus("Earth"));
            Assert.IsTrue(character.HasStatus("Blur"));
            Assert.AreEqual(7, character.CurrentMovementSpeed);

            _commandManager.Undo();
            Assert.IsTrue(character.HasStatus("Wind"));
            Assert.IsFalse(character.HasStatus("Earth"));
            Assert.IsFalse(character.HasStatus("Blur"));
            Assert.AreEqual(5, character.CurrentMovementSpeed);
        }

        /// <summary>
        /// Verifies terrain transformations work properly and are fully undoable.
        /// </summary>
        [Test]
        public void When_SpellsCastOnTerrain_Expect_TerrainTransformationsAndRollbackOnUndo()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            ITile tile = _grid.GetTile(new GridPosition(1, 1));
            
            // Ground + Ice -> Mud
            _commandManager.ExecuteCommand(new CastSpellCommand(new GridPosition(1, 1), "Ice", _grid, CharacterRegistry.AllCharacters));
            Assert.AreEqual(TerrainType.Mud, tile.CurrentTerrain);

            _commandManager.Undo();
            Assert.AreEqual(TerrainType.Ground, tile.CurrentTerrain);

            // Water + Ice -> Ice
            tile.SetTerrain(TerrainType.Water);
            _commandManager.ExecuteCommand(new CastSpellCommand(new GridPosition(1, 1), "Ice", _grid, CharacterRegistry.AllCharacters));
            Assert.AreEqual(TerrainType.Ice, tile.CurrentTerrain);

            _commandManager.Undo();
            Assert.AreEqual(TerrainType.Water, tile.CurrentTerrain);
        }

        /// <summary>
        /// Verifies that Fire + Wind spreads fire to adjacent tiles and is undoable.
        /// </summary>
        [Test]
        public void When_FireAndWindSpellCast_Expect_FireSpreadsToNeighborsAndRollbackOnUndo()
        {
            var character = new Character("Hero", new GridPosition(2, 2), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            ITile target = _grid.GetTile(new GridPosition(2, 2));
            target.SetTerrain(TerrainType.Fire);

            ITile neighbor = _grid.GetTile(new GridPosition(2, 3));
            neighbor.SetTerrain(TerrainType.Grass);
            Assert.AreEqual(TerrainType.Grass, neighbor.CurrentTerrain);

            _commandManager.ExecuteCommand(new CastSpellCommand(new GridPosition(2, 2), "Wind", _grid, CharacterRegistry.AllCharacters));
            
            Assert.AreEqual(TerrainType.Fire, neighbor.CurrentTerrain);

            _commandManager.Undo();
            Assert.AreEqual(TerrainType.Grass, neighbor.CurrentTerrain);
        }

        /// <summary>
        /// Verifies that turn transitions process end-of-turn actions correctly.
        /// </summary>
        [Test]
        public void When_EndTurnCalled_Expect_HPRegenOnGrass()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            _grid.GetTile(new GridPosition(0, 0)).SetTerrain(TerrainType.Grass);
            character.ApplyDamage(20);
            character.AddStatus("Fire");

            Assert.AreEqual(80, character.CurrentHP);

            _commandManager.ExecuteCommand(new TurnTransitionCommand(CharacterRegistry.AllCharacters, _grid));

            // Standing on Grass heals 10, Fire status tick is omitted -> net HP change = +10 (total 90)
            Assert.AreEqual(90, character.CurrentHP);

            _commandManager.Undo();
            Assert.AreEqual(80, character.CurrentHP);
        }

        /// <summary>
        /// Verifies that EndTurnCommand correctly cures Freeze status and restores the character's movement speed after 1 turn.
        /// </summary>
        [Test]
        public void When_EndTurnCalledWithFreeze_Expect_CuringFreezeAndRestoringSpeed()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            character.AddStatus("Freeze");
            character.ModifyMovementSpeed(-character.CurrentMovementSpeed);

            Assert.AreEqual(0, character.CurrentMovementSpeed);

            // 1. First turn transition (decrements to 0, status expires, speed restored)
            _commandManager.ExecuteCommand(new TurnTransitionCommand(CharacterRegistry.AllCharacters, _grid));
            Assert.IsFalse(character.HasStatus("Freeze"));
            Assert.AreEqual(5, character.CurrentMovementSpeed);

            // 2. Undo turn transition (restores status)
            _commandManager.Undo();
            Assert.IsTrue(character.HasStatus("Freeze"));
            Assert.AreEqual(0, character.CurrentMovementSpeed);
        }

        /// <summary>
        /// Verifies that a character carrying a reaction status cannot gain any other status effect.
        /// </summary>
        [Test]
        public void When_CharacterHasReactionStatus_Expect_CannotGainOtherStatuses()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            // Apply a reaction status (Freeze)
            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Freeze"));
            Assert.IsTrue(character.HasStatus("Freeze"));

            // Tries to apply another status (Fire)
            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Fire"));

            // Character should NOT have Fire status since Freeze blocks status gains
            Assert.IsFalse(character.HasStatus("Fire"));
            Assert.IsTrue(character.HasStatus("Freeze"));

            _commandManager.Undo(); // Undo Fire try (does nothing, clean)
            _commandManager.Undo(); // Undo Freeze
            Assert.IsFalse(character.HasStatus("Freeze"));
        }

        /// <summary>
        /// Verifies that statuses have a duration of 1 turn, expiring at the first turn transition.
        /// </summary>
        [Test]
        public void When_StatusApplied_Expect_ExpiresAtFirstEndTurn()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            // Apply Fire status (should get initial duration of 1)
            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Fire"));
            Assert.IsTrue(character.HasStatus("Fire"));
            Assert.AreEqual(1, character.GetStatusDuration("Fire"));

            // 1. First turn transition (decrements to 0, status expires/disappears)
            _commandManager.ExecuteCommand(new TurnTransitionCommand(CharacterRegistry.AllCharacters, _grid));
            Assert.IsFalse(character.HasStatus("Fire"));
            Assert.AreEqual(0, character.GetStatusDuration("Fire"));

            // 2. Undo turn transition (restores status with duration 1)
            _commandManager.Undo();
            Assert.IsTrue(character.HasStatus("Fire"));
            Assert.AreEqual(1, character.GetStatusDuration("Fire"));

            // 3. Undo initial application (removes status)
            _commandManager.Undo();
            Assert.IsFalse(character.HasStatus("Fire"));
        }

        /// <summary>
        /// Verifies a complex multi-turn flow of spell casting (3x3 AoE), reaction triggers, reaction status immunity blocks, 
        /// multiple turn transitions (decay/expiry), and a full undo/redo cycle.
        /// </summary>
        [Test]
        public void When_ComplexSpellAndReactionTurnFlowExecutedAndUndone_Expect_AllStatesSynchronizeCorrectly()
        {
            var hero = new Character("Hero", new GridPosition(2, 2), 100, 5);
            var enemy = new Character("Enemy", new GridPosition(2, 3), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { hero, enemy };
            GridRegistry.Grid = _grid;

            ITile centerTile = _grid.GetTile(new GridPosition(2, 2));

            // Step 1: Cast Ice Spell at (2, 2)
            // Expect: (2,2) Ground -> Mud, Hero and Enemy gain Ice (duration 2)
            _commandManager.ExecuteCommand(new CastSpellCommand(new GridPosition(2, 2), "Ice", _grid, CharacterRegistry.AllCharacters));
            Assert.AreEqual(TerrainType.Mud, centerTile.CurrentTerrain);
            Assert.IsTrue(hero.HasStatus("Ice"));
            Assert.IsTrue(enemy.HasStatus("Ice"));

            // Step 2: Cast Earth Spell at (2, 2)
            // Expect: Hero and Enemy react Ice + Earth -> Freeze (duration 2, speed 0)
            _commandManager.ExecuteCommand(new CastSpellCommand(new GridPosition(2, 2), "Earth", _grid, CharacterRegistry.AllCharacters));
            Assert.IsFalse(hero.HasStatus("Ice"));
            Assert.IsFalse(hero.HasStatus("Earth"));
            Assert.IsTrue(hero.HasStatus("Freeze"));
            Assert.AreEqual(0, hero.CurrentMovementSpeed);
            Assert.IsTrue(enemy.HasStatus("Freeze"));
            Assert.AreEqual(0, enemy.CurrentMovementSpeed);

            // Step 3: Try casting Fire Spell at (2, 2)
            // Expect: Blocked by Freeze status immunity. Hero and Enemy do NOT gain Fire.
            _commandManager.ExecuteCommand(new CastSpellCommand(new GridPosition(2, 2), "Fire", _grid, CharacterRegistry.AllCharacters));
            Assert.IsFalse(hero.HasStatus("Fire"));
            Assert.IsFalse(enemy.HasStatus("Fire"));

            // Step 4: First turn transition (Space)
            // Expect: Freeze duration decrements to 0 -> expires, speed restored to 5.
            _commandManager.ExecuteCommand(new TurnTransitionCommand(CharacterRegistry.AllCharacters, _grid));
            Assert.IsFalse(hero.HasStatus("Freeze"));
            Assert.AreEqual(5, hero.CurrentMovementSpeed);
            Assert.IsFalse(enemy.HasStatus("Freeze"));
            Assert.AreEqual(5, enemy.CurrentMovementSpeed);

            // --- UNDO SWEEP ---
            // Undo Turn 1 transition -> Freeze restored with duration 1, speed 0
            _commandManager.Undo();
            Assert.IsTrue(hero.HasStatus("Freeze"));
            Assert.AreEqual(1, hero.GetStatusDuration("Freeze"));
            Assert.AreEqual(0, hero.CurrentMovementSpeed);

            // Undo Fire Spell cast try -> no visible change since it was blocked
            _commandManager.Undo();
            Assert.IsFalse(hero.HasStatus("Fire"));

            // Undo Earth Spell cast -> Freeze removed, Ice and Earth restored, speed back to 5
            _commandManager.Undo();
            Assert.IsFalse(hero.HasStatus("Freeze"));
            Assert.IsTrue(hero.HasStatus("Ice"));
            Assert.AreEqual(5, hero.CurrentMovementSpeed);

            // Undo Ice Spell cast -> Mud reverts to Ground, Ice status removed
            _commandManager.Undo();
            Assert.AreEqual(TerrainType.Ground, centerTile.CurrentTerrain);
            Assert.IsFalse(hero.HasStatus("Ice"));
            Assert.IsFalse(enemy.HasStatus("Ice"));

            // --- REDO SWEEP ---
            // Redo Ice Spell
            _commandManager.Redo();
            Assert.AreEqual(TerrainType.Mud, centerTile.CurrentTerrain);
            Assert.IsTrue(hero.HasStatus("Ice"));

            // Redo Earth Spell -> Freeze
            _commandManager.Redo();
            Assert.IsTrue(hero.HasStatus("Freeze"));
            Assert.AreEqual(0, hero.CurrentMovementSpeed);

            // Redo Fire Spell try
            _commandManager.Redo();
            Assert.IsFalse(hero.HasStatus("Fire"));

            // Redo Turn 1 transition -> Freeze expires, speed restored
            _commandManager.Redo();
            Assert.IsFalse(hero.HasStatus("Freeze"));
            Assert.AreEqual(5, hero.CurrentMovementSpeed);
            Assert.IsFalse(enemy.HasStatus("Freeze"));
            Assert.AreEqual(5, enemy.CurrentMovementSpeed);
        }

        /// <summary>
        /// Verifies that character movement consumes their entire speed budget, and undoing restores it.
        /// </summary>
        [Test]
        public void When_CharacterMoves_Expect_SpeedBudgetExhausted()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            // Path from 0,0 to 0,2 (2 steps on Ground, cost = 2)
            List<GridPosition> path = new List<GridPosition> { new GridPosition(0, 1), new GridPosition(0, 2) };
            
            var moveCommand = new MoveCharacterCommand(character, new GridPosition(0, 2), path, _grid);
            _commandManager.ExecuteCommand(moveCommand);

            // Speed budget should be completely exhausted (0)
            Assert.AreEqual(0, character.CurrentMovementSpeed);

            _commandManager.Undo();
            Assert.AreEqual(5, character.CurrentMovementSpeed);
        }

        /// <summary>
        /// Verifies that executing a turn transition restores all characters' movement budgets.
        /// </summary>
        [Test]
        public void When_TurnTransitionCalled_Expect_SpeedBudgetRecovered()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            // Reduce speed to 1
            character.ModifyMovementSpeed(-4);
            Assert.AreEqual(1, character.CurrentMovementSpeed);

            // Turn transition
            _commandManager.ExecuteCommand(new TurnTransitionCommand(CharacterRegistry.AllCharacters, _grid));

            // Speed budget should recover to base speed (5)
            Assert.AreEqual(5, character.CurrentMovementSpeed);

            _commandManager.Undo();
            Assert.AreEqual(1, character.CurrentMovementSpeed);
        }

        /// <summary>
        /// Verifies that a character with 0 speed has 0 reachable tiles.
        /// </summary>
        [Test]
        public void When_CharacterMovementExhausted_Expect_ReachableTilesEmpty()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            var pathfinder = new DijkstraPathfinder();

            // Exhaust speed budget to 0
            character.ModifyMovementSpeed(-5);
            Assert.AreEqual(0, character.CurrentMovementSpeed);

            // Find reachable tiles
            HashSet<GridPosition> reachable = pathfinder.GetReachableTiles(_grid, character.Position, (int)character.CurrentMovementSpeed);

            // Reachable set should contain only their starting position (cost 0) or be empty depending on design.
            // Since cost to stay at start is 0, the starting tile itself is reachable. But other tiles should be unreachable.
            Assert.IsTrue(reachable.Contains(new GridPosition(0, 0)));
            Assert.IsFalse(reachable.Contains(new GridPosition(0, 1)));
            Assert.IsFalse(reachable.Contains(new GridPosition(1, 0)));
        }

        /// <summary>
        /// Verifies that Magic damage dealt on a Tree tile is amplified by 50%.
        /// </summary>
        [Test]
        public void When_MagicDamageDealtOnTree_Expect_AmplifiedBy50Percent()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            _grid.GetTile(new GridPosition(0, 0)).SetTerrain(TerrainType.Tree);

            _commandManager.ExecuteCommand(new DamageCharacterCommand(character, 20, DamageType.Magic));

            // Magic damage 20 should be amplified by 1.5x to 30. HP = 100 - 30 = 70.
            Assert.AreEqual(70, character.CurrentHP);

            _commandManager.Undo();
            Assert.AreEqual(100, character.CurrentHP);
        }

        /// <summary>
        /// Verifies that Magic damage dealt with MudTraversal status is reduced by 50%.
        /// </summary>
        [Test]
        public void When_MagicDamageDealtWithMudTraversal_Expect_ReducedBy50Percent()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            character.AddStatus("MudTraversal");

            _commandManager.ExecuteCommand(new DamageCharacterCommand(character, 20, DamageType.Magic));

            // Magic damage 20 should be reduced by 50% to 10. HP = 100 - 10 = 90.
            Assert.AreEqual(90, character.CurrentHP);

            _commandManager.Undo();
            Assert.AreEqual(100, character.CurrentHP);
        }

        /// <summary>
        /// Verifies that Physical damage dealt on a Water tile is reduced by 25%.
        /// </summary>
        [Test]
        public void When_PhysicalDamageDealtOnWater_Expect_ReducedBy25Percent()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            _grid.GetTile(new GridPosition(0, 0)).SetTerrain(TerrainType.Water);

            _commandManager.ExecuteCommand(new DamageCharacterCommand(character, 20, DamageType.Physical));

            // Physical damage 20 should be reduced by 25% to 15. HP = 100 - 15 = 85.
            Assert.AreEqual(85, character.CurrentHP);

            _commandManager.Undo();
            Assert.AreEqual(100, character.CurrentHP);
        }

        /// <summary>
        /// Verifies that stopping on an Ice tile reduces evasion by 20, and moving off restores it.
        /// </summary>
        [Test]
        public void When_CharacterStopsOnIce_Expect_EvasionReducedBy20()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            _grid.GetTile(new GridPosition(0, 1)).SetTerrain(TerrainType.Ice);

            // Path to Ice tile at 0,1
            List<GridPosition> path = new List<GridPosition> { new GridPosition(0, 1) };
            _commandManager.ExecuteCommand(new MoveCharacterCommand(character, new GridPosition(0, 1), path, _grid));

            // Evasion modifier should be -20
            Assert.AreEqual(-20, character.EvasionModifier);

            _commandManager.Undo();
            Assert.AreEqual(0, character.EvasionModifier);
        }

        /// <summary>
        /// Verifies that executing a melee attack while FireImbued deals additional magic damage.
        /// </summary>
        [Test]
        public void When_MeleeAttackExecutedWithFireImbue_Expect_AdditionalMagicDamage()
        {
            var attacker = new Character("Hero", new GridPosition(0, 0), 100, 5);
            var target = new Character("Enemy", new GridPosition(0, 1), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { attacker, target };
            GridRegistry.Grid = _grid;

            attacker.AddStatus("FireImbued");

            _commandManager.ExecuteCommand(new MeleeAttackCommand(attacker, target));

            // Target takes 20 physical damage + 5 magic damage = 25 damage total. HP = 100 - 25 = 75.
            Assert.AreEqual(75, target.CurrentHP);

            _commandManager.Undo();
            Assert.AreEqual(100, target.CurrentHP);
        }

        /// <summary>
        /// Verifies that executing a turn transition with lock history enabled clears the command manager stack,
        /// locking in all previous turn decisions.
        /// </summary>
        [Test]
        public void When_TurnTransitionExecutedWithLockEnabled_Expect_HistoryClearedAndCannotUndo()
        {
            var character = new Character("Hero", new GridPosition(0, 0), 100, 5);
            CharacterRegistry.AllCharacters = new List<ICharacter> { character };
            GridRegistry.Grid = _grid;

            // Perform some action (e.g., apply a status)
            _commandManager.ExecuteCommand(new ApplyStatusEffectCommand(character, "Fire"));
            Assert.IsTrue(_commandManager.CanUndo);

            // Execute TurnTransitionCommand with lockHistory: true
            var turnCommand = new TurnTransitionCommand(CharacterRegistry.AllCharacters, _grid, _commandManager, lockHistory: true);
            _commandManager.ExecuteCommand(turnCommand);

            // Expect: History is cleared, and we cannot undo previous actions
            Assert.IsFalse(_commandManager.CanUndo);
            Assert.IsFalse(_commandManager.CanRedo);
        }

        /// <summary>
        /// Verifies that executing a turn transition in a multi-team setup alternates the active team.
        /// </summary>
        [Test]
        public void When_TurnTransitionsInMultiTeamSetup_Expect_ActiveTeamAlternates()
        {
            var player = new Character("Hero", new GridPosition(0, 0), 100, 5, Team.Player);
            var enemy = new Character("Enemy", new GridPosition(0, 1), 100, 5, Team.Enemy);
            CharacterRegistry.AllCharacters = new List<ICharacter> { player, enemy };
            GridRegistry.Grid = _grid;

            TurnRegistry.ActiveTeam = Team.Player;

            // Transition turn (no history lock in test so we can undo)
            var turnCommand = new TurnTransitionCommand(CharacterRegistry.AllCharacters, _grid, _commandManager, lockHistory: false);
            _commandManager.ExecuteCommand(turnCommand);

            // Active team should switch to Enemy
            Assert.AreEqual(Team.Enemy, TurnRegistry.ActiveTeam);

            // Undo transition
            _commandManager.Undo();

            // Active team should roll back to Player
            Assert.AreEqual(Team.Player, TurnRegistry.ActiveTeam);
        }

        /// <summary>
        /// Verifies that turn-start effects (like Grass healing) only apply to the starting team's characters.
        /// </summary>
        [Test]
        public void When_StartTurnCalledForPlayer_Expect_OnlyPlayerGrassHealAndSpeedReset()
        {
            var player = new Character("Hero", new GridPosition(0, 0), 100, 5, Team.Player); // stands on Grass
            player.ApplyDamage(10); // current HP is 90/100
            var enemy = new Character("Enemy", new GridPosition(0, 1), 100, 5, Team.Enemy);   // stands on Grass
            enemy.ApplyDamage(10); // current HP is 90/100
            CharacterRegistry.AllCharacters = new List<ICharacter> { player, enemy };
            GridRegistry.Grid = _grid;

            // Make sure both are standing on Grass tiles
            _grid.GetTile(new GridPosition(0, 0)).SetTerrain(TerrainType.Grass);
            _grid.GetTile(new GridPosition(0, 1)).SetTerrain(TerrainType.Grass);

            // Reduce movement speeds to simulate spent budgets
            player.ModifyMovementSpeed(-3);
            enemy.ModifyMovementSpeed(-3);

            // Execute StartTurnCommand for Team.Player
            var startCommand = new StartTurnCommand(CharacterRegistry.AllCharacters, _grid, Team.Player);
            _commandManager.ExecuteCommand(startCommand);

            // Player character should be healed and speed reset
            Assert.AreEqual(100, player.CurrentHP);
            Assert.AreEqual(5, player.CurrentMovementSpeed);

            // Enemy character should remain unchanged
            Assert.AreEqual(90, enemy.CurrentHP);
            Assert.AreEqual(2, enemy.CurrentMovementSpeed);

            // Undo command
            _commandManager.Undo();
            Assert.AreEqual(90, player.CurrentHP);
            Assert.AreEqual(2, player.CurrentMovementSpeed);
        }

        /// <summary>
        /// Verifies that status duration ticks only apply to the ending team's characters.
        /// </summary>
        [Test]
        public void When_EndTurnCalledForPlayer_Expect_OnlyPlayerStatusDurationDecrements()
        {
            var player = new Character("Hero", new GridPosition(0, 0), 100, 5, Team.Player);
            var enemy = new Character("Enemy", new GridPosition(0, 1), 100, 5, Team.Enemy);
            CharacterRegistry.AllCharacters = new List<ICharacter> { player, enemy };
            GridRegistry.Grid = _grid;

            // Apply "Fire" status to both characters (adds with duration = 1)
            player.AddStatus("Fire");
            enemy.AddStatus("Fire");
            Assert.AreEqual(1, player.GetStatusDuration("Fire"));
            Assert.AreEqual(1, enemy.GetStatusDuration("Fire"));

            // Execute EndTurnCommand for Team.Player
            var endCommand = new EndTurnCommand(CharacterRegistry.AllCharacters, _grid, Team.Player);
            _commandManager.ExecuteCommand(endCommand);

            // Player's Fire status should decrement to 0 and expire
            Assert.AreEqual(0, player.GetStatusDuration("Fire"));
            Assert.IsFalse(player.HasStatus("Fire"));

            // Enemy's Fire status should remain at 1
            Assert.AreEqual(1, enemy.GetStatusDuration("Fire"));
            Assert.IsTrue(enemy.HasStatus("Fire"));

            // Undo command (restores player status to 1)
            _commandManager.Undo();
            Assert.AreEqual(1, player.GetStatusDuration("Fire"));
            Assert.IsTrue(player.HasStatus("Fire"));
        }
    }
}
