using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Core.Commands;
using Core.Model;

namespace View
{
    public class DemoController : MonoBehaviour
    {
        public enum InputState
        {
            Normal,
            ActionMenu,
            TargetingAttack,
            TargetingSpell
        }

        [Header("References")]
        [SerializeField] private GridView _gridView;
        [SerializeField] private Tilemap _tilemap;

        [Header("Prefabs & Sprites")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private Sprite _cursorSprite;

        [Header("UI Canvas Action Menu References")]
        [SerializeField] private GameObject _actionMenuPanel;
        [SerializeField] private Button _attackButton;
        [SerializeField] private Button _waitButton;
        [Header("Mage Spell Buttons")]
        [SerializeField] private Button _iceButton;
        [SerializeField] private Button _fireButton;
        [SerializeField] private Button _earthButton;
        [SerializeField] private Button _windButton;

        private ICommandManager _commandManager;
        private List<ICharacter> _characters = new List<ICharacter>();
        private List<CharacterView> _characterViews = new List<CharacterView>();
        private ICharacter _selectedCharacter;
        private IPathfinder _pathfinder;
        private bool _wasCharacterMoving;
        private GameObject _cursorInstance;

        private InputState _currentInputState = InputState.Normal;
        private string _pendingSpellName = "";

        /// <summary>
        /// Unity Start event. Initializes command manager, pathfinder, references, and scene entities.
        /// </summary>
        private void Start()
        {
            _commandManager = new CommandManager();
            _pathfinder = new DijkstraPathfinder();

            if (_gridView == null)
            {
                _gridView = FindAnyObjectByType<GridView>();
            }

            if (_tilemap == null)
            {
                _tilemap = _gridView.GetComponentInChildren<Tilemap>();
            }

            if (_gridView != null && _gridView.GridModel != null)
            {
                InitializeSceneEntities();
            }
            else
            {
                Debug.LogError("[DemoController] GridView or GridModel is missing! Make sure GridView is initialized.");
            }

            // Hide action menu panel on startup
            if (_actionMenuPanel != null)
            {
                _actionMenuPanel.SetActive(false);
            }

            // Bind listeners for wait, melee attack, and spells
            if (_waitButton != null) _waitButton.onClick.AddListener(HandleWaitClicked);
            if (_attackButton != null) _attackButton.onClick.AddListener(HandleAttackClicked);
            if (_iceButton != null) _iceButton.onClick.AddListener(() => HandleSpellClicked("Ice"));
            if (_fireButton != null) _fireButton.onClick.AddListener(() => HandleSpellClicked("Fire"));
            if (_earthButton != null) _earthButton.onClick.AddListener(() => HandleSpellClicked("Earth"));
            if (_windButton != null) _windButton.onClick.AddListener(() => HandleSpellClicked("Wind"));
        }

        /// <summary>
        /// Instantiates models and visual game objects for the characters and selection cursor.
        /// </summary>
        private void InitializeSceneEntities()
        {
            _characters.Clear();
            _characterViews.Clear();

            // 1. Search the active scene for any pre-placed character views
            CharacterView[] preplacedViews = FindObjectsByType<CharacterView>();
            Debug.Log($"[DemoController] preplacedViews count: {preplacedViews?.Length ?? 0}");

            Dictionary<string, int> nameCounts = new Dictionary<string, int>();
            string GetUniqueName(string baseName)
            {
                if (string.IsNullOrEmpty(baseName)) baseName = "Unit";
                if (!nameCounts.ContainsKey(baseName))
                {
                    nameCounts[baseName] = 1;
                    return baseName;
                }
                nameCounts[baseName]++;
                return $"{baseName} {nameCounts[baseName]}";
            }

            if (preplacedViews != null && preplacedViews.Length > 0)
            {
                BoundsInt bounds = _tilemap.cellBounds;

                foreach (var view in preplacedViews)
                {
                    Debug.Log($"[DemoController] Found pre-placed CharacterView on GameObject: '{view.gameObject.name}' at world position: {view.transform.position}");
                    
                    // Project the world position of the preplaced view onto grid tile coordinates
                    Vector3Int cellPos = _tilemap.WorldToCell(view.transform.position);
                    GridPosition startGridPos = new GridPosition(cellPos.x - bounds.xMin, cellPos.y - bounds.yMin);

                    if (!_gridView.GridModel.IsValidPosition(startGridPos))
                    {
                        Debug.LogWarning($"[DemoController] Preplaced unit '{view.gameObject.name}' maps to out-of-bounds coordinate {startGridPos}! Snapping to (0,0).");
                        startGridPos = new GridPosition(0, 0);
                    }

                    string name = GetUniqueName(view.CharacterName);
                    var model = new Character(name, startGridPos, view.MaxHP, view.MovementBudget, view.Team, view.CharacterType);
                    
                    view.Initialize(model, _gridView, _tilemap);
                    _characters.Add(model);
                    _characterViews.Add(view);
                    Debug.Log($"[DemoController] Initialized pre-placed unit '{name}' ({view.Team}) at grid pos: {startGridPos}");
                }
            }
            else
            {
                // Fallback: Spawn default characters if none are in the scene
                Debug.Log("[DemoController] No pre-placed units found in the scene. Spawning default units...");

                ICharacter heroModel = null;
                ICharacter allyModel = null;

                if (_playerPrefab != null || _enemyPrefab != null)
                {
                    if (_playerPrefab != null)
                    {
                        Debug.Log($"[DemoController] Spawning default player using prefab '{_playerPrefab.name}'");
                        GameObject heroGo = Instantiate(_playerPrefab);
                        heroGo.name = "Visual_Hero";
                        var heroRenderer = heroGo.GetComponentInChildren<SpriteRenderer>();
                        if (heroRenderer != null)
                        {
                            heroRenderer.sortingOrder = 5;
                        }
                        var heroView = heroGo.GetComponent<CharacterView>();
                        if (heroView == null)
                        {
                            heroView = heroGo.AddComponent<CharacterView>();
                        }
                        
                        string name = GetUniqueName(heroView.CharacterName);
                        heroModel = new Character(name, new GridPosition(0, 0), heroView.MaxHP, heroView.MovementBudget, Team.Player, heroView.CharacterType);
                        heroView.Initialize(heroModel, _gridView, _tilemap);
                        _characterViews.Add(heroView);
                        Debug.Log($"[DemoController] Spawned Hero prefab as '{name}' (Player) at grid (0,0), world pos: {heroGo.transform.position}");
                    }

                    GameObject enemySource = _enemyPrefab != null ? _enemyPrefab : _playerPrefab;
                    if (enemySource != null)
                    {
                        Debug.Log($"[DemoController] Spawning default enemy using prefab '{enemySource.name}'");
                        GameObject allyGo = Instantiate(enemySource);
                        allyGo.name = "Visual_Ally";
                        var allyRenderer = allyGo.GetComponentInChildren<SpriteRenderer>();
                        if (allyRenderer != null)
                        {
                            allyRenderer.sortingOrder = 5;
                            if (_enemyPrefab == null)
                            {
                                allyRenderer.color = new Color(1f, 0.6f, 0.6f, 1f); // red tint if sharing player prefab
                            }
                        }
                        var allyView = allyGo.GetComponent<CharacterView>();
                        if (allyView == null)
                        {
                            allyView = allyGo.AddComponent<CharacterView>();
                        }
                        string allyName = GetUniqueName(allyView.CharacterName);
                        allyModel = new Character(allyName, new GridPosition(0, 2), allyView.MaxHP, allyView.MovementBudget, Team.Enemy, allyView.CharacterType);
                        allyView.Initialize(allyModel, _gridView, _tilemap);
                        _characterViews.Add(allyView);
                        Debug.Log($"[DemoController] Spawned Ally prefab as '{allyName}' (Enemy) at grid (0,2), world pos: {allyGo.transform.position}");
                    }
                }
                else
                {
                    Debug.Log("[DemoController] Spawning default units using procedural sprite manual fallback (no prefab assigned).");
                    // Fallback to manual setup with default stats if no prefab is assigned
                    string heroName = GetUniqueName("Hero");
                    string allyName = GetUniqueName("Hero");
                    heroModel = new Character(heroName, new GridPosition(0, 0), 100, 5, Team.Player, CharacterType.Warrior);
                    allyModel = new Character(allyName, new GridPosition(0, 2), 100, 5, Team.Enemy, CharacterType.Warrior);

                    Sprite defaultSprite = CreateDefaultSprite();

                    GameObject heroGo = new GameObject("Visual_Hero");
                    var heroRenderer = heroGo.AddComponent<SpriteRenderer>();
                    heroRenderer.sprite = defaultSprite;
                    heroRenderer.sortingOrder = 5;
                    var heroView = heroGo.AddComponent<CharacterView>();
                    heroView.Initialize(heroModel, _gridView, _tilemap);
                    _characterViews.Add(heroView);
                    Debug.Log($"[DemoController] Spawned fallback Hero '{heroName}' (Player) at grid (0,0), world pos: {heroGo.transform.position}");

                    GameObject allyGo = new GameObject("Visual_Ally");
                    var allyRenderer = allyGo.AddComponent<SpriteRenderer>();
                    allyRenderer.sprite = defaultSprite;
                    allyRenderer.color = new Color(1f, 0.6f, 0.6f, 1f); // red tint for enemy
                    allyRenderer.sortingOrder = 5;
                    var allyView = allyGo.AddComponent<CharacterView>();
                    allyView.Initialize(allyModel, _gridView, _tilemap);
                    _characterViews.Add(allyView);
                    Debug.Log($"[DemoController] Spawned fallback Ally '{allyName}' (Enemy) at grid (0,2), world pos: {allyGo.transform.position}");
                }

                _characters.Add(heroModel);
                _characters.Add(allyModel);
            }

            // Setup cursor
            _cursorInstance = new GameObject("Visual_Cursor");
            var cursorRenderer = _cursorInstance.AddComponent<SpriteRenderer>();
            cursorRenderer.sprite = _cursorSprite;
            cursorRenderer.sortingOrder = 10;
            _cursorInstance.transform.localScale = new Vector3(1f, 1f, 1f);

            // Register in static registries
            CharacterRegistry.AllCharacters = new List<ICharacter>(_characters);
            GridRegistry.Grid = _gridView.GridModel;

            // Do not select any unit by default on startup
            _selectedCharacter = null;

            Debug.Log($"[DemoController] Scene initialized with {_characters.Count} characters. Selected unit: {_selectedCharacter?.Name}");
            UpdateReachableTileHighlights();
        }

        /// <summary>
        /// Unity Update event. Handles cursor positioning, input, and movement monitoring.
        /// </summary>
        private void Update()
        {
            if (_gridView == null || _gridView.GridModel == null || _characters.Count == 0) return;

            UpdateCursorPosition();
            HandleInput();

            bool isAnyMoving = false;
            foreach (var charView in _characterViews)
            {
                if (charView != null && charView.IsMoving)
                {
                    isAnyMoving = true;
                }
            }

            if (_wasCharacterMoving && !isAnyMoving)
            {
                UpdateReachableTileHighlights();

                // If a character finishes moving, open their Action Menu!
                if (_selectedCharacter != null)
                {
                    OpenActionMenu(_selectedCharacter);
                }
            }
            _wasCharacterMoving = isAnyMoving;
        }

        /// <summary>
        /// Obtains the world coordinate of the mouse pointer projected on the 2D plane.
        /// </summary>
        private Vector3 GetMouseWorldPosition()
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            if (Camera.main != null)
            {
                mouseScreenPos.z = Mathf.Abs(Camera.main.transform.position.z);
                return Camera.main.ScreenToWorldPoint(mouseScreenPos);
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Position the selection cursor over the tile matching the mouse coordinates, or hides it if out of bounds.
        /// </summary>
        private void UpdateCursorPosition()
        {
            if (Camera.main == null) return;

            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector3Int cellPos = _tilemap.WorldToCell(mouseWorldPos);
            cellPos.z = 0;

            BoundsInt bounds = _tilemap.cellBounds;
            GridPosition hoverPos = new GridPosition(cellPos.x - bounds.xMin, cellPos.y - bounds.yMin);

            if (_gridView.GridModel.IsValidPosition(hoverPos))
            {
                _cursorInstance.SetActive(true);
                _cursorInstance.transform.position = _tilemap.GetCellCenterWorld(cellPos);
            }
            else
            {
                _cursorInstance.SetActive(false);
            }
        }

        /// <summary>
        /// Monitors mouse clicks, keyboard hotkeys (U/R/1-8) and performs character select/move, spells, undo/redo, or turn transitions.
        /// </summary>
        private void HandleInput()
        {
            // Block input while any character is moving
            foreach (var charView in _characterViews)
            {
                if (charView != null && charView.IsMoving) return;
            }

            // Space key triggers turn transitions (end of current turn + start of next turn)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                CloseActionMenu();
                DeselectCharacter();
                var turnCommand = new TurnTransitionCommand(CharacterRegistry.AllCharacters, _gridView.GridModel, _commandManager, lockHistory: true);
                _commandManager.ExecuteCommand(turnCommand);
                Debug.Log($"[DemoController] Turn transitioned. Active team: {TurnRegistry.ActiveTeam}");
                return;
            }

            // If it is currently the Enemy's turn, lock out all other player actions
            if (TurnRegistry.ActiveTeam != Team.Player)
            {
                return;
            }

            // If ActionMenu is open, block all other interactive keyboard/mouse inputs
            if (_currentInputState == InputState.ActionMenu)
            {
                return;
            }

            // Targeting mode for Melee Attack
            if (_currentInputState == InputState.TargetingAttack)
            {
                if (Input.GetMouseButtonDown(0) && Camera.main != null)
                {
                    GridPosition targetPos = GetHoveredGridPosition();
                    ICharacter targetChar = GetCharacterAt(targetPos);
                    
                    if (targetChar != null && targetChar.Team != _selectedCharacter.Team && IsAdjacent(_selectedCharacter.Position, targetPos))
                    {
                        var attackCommand = new MeleeAttackCommand(_selectedCharacter, targetChar);
                        _commandManager.ExecuteCommand(attackCommand);

                        if (_selectedCharacter.CurrentMovementSpeed > 0f)
                        {
                            var spendSpeed = new ModifyMovementSpeedCommand(_selectedCharacter, -_selectedCharacter.CurrentMovementSpeed);
                            _commandManager.ExecuteCommand(spendSpeed);
                        }
                        
                        DeselectCharacter();
                        _currentInputState = InputState.Normal;
                    }
                    else
                    {
                        // Cancel targeting, clear highlights, and return to Action Menu
                        _gridView.ClearHighlights();
                        OpenActionMenu(_selectedCharacter);
                    }
                }
                return;
            }

            // Targeting mode for Magic Spells
            if (_currentInputState == InputState.TargetingSpell)
            {
                if (Input.GetMouseButtonDown(0) && Camera.main != null)
                {
                    GridPosition targetPos = GetHoveredGridPosition();
                    if (_gridView.GridModel.IsValidPosition(targetPos))
                    {
                        var command = new CastSpellCommand(targetPos, _pendingSpellName, _gridView.GridModel, CharacterRegistry.AllCharacters);
                        _commandManager.ExecuteCommand(command);
                        Debug.Log($"[DemoController] Cast {_pendingSpellName} spell at {targetPos}.");

                        if (_selectedCharacter.CurrentMovementSpeed > 0f)
                        {
                            var spendSpeed = new ModifyMovementSpeedCommand(_selectedCharacter, -_selectedCharacter.CurrentMovementSpeed);
                            _commandManager.ExecuteCommand(spendSpeed);
                        }

                        DeselectCharacter();
                        _currentInputState = InputState.Normal;
                    }
                    else
                    {
                        // Cancel targeting, clear highlights, and return to Action Menu
                        _gridView.ClearHighlights();
                        OpenActionMenu(_selectedCharacter);
                    }
                }
                return;
            }

            if (Input.GetMouseButtonDown(0) && Camera.main != null)
            {
                Vector3 mouseWorldPos = GetMouseWorldPosition();
                Vector3Int cellPos = _tilemap.WorldToCell(mouseWorldPos);
                cellPos.z = 0;

                BoundsInt bounds = _tilemap.cellBounds;
                GridPosition targetPos = new GridPosition(cellPos.x - bounds.xMin, cellPos.y - bounds.yMin);

                if (_gridView.GridModel.IsValidPosition(targetPos))
                {
                    ICharacter clickedChar = GetCharacterAt(targetPos);

                    if (clickedChar != null)
                    {
                        if (clickedChar == _selectedCharacter)
                        {
                            // Clicked the selected character again -> Open Action Menu!
                            OpenActionMenu(_selectedCharacter);
                        }
                        else
                        {
                            if (clickedChar.Team != TurnRegistry.ActiveTeam)
                            {
                                Debug.Log($"[DemoController] Cannot select {clickedChar.Name} - it is currently the {TurnRegistry.ActiveTeam} team's turn!");
                                return;
                            }

                            if (clickedChar.CurrentMovementSpeed <= 0f)
                            {
                                Debug.Log($"[DemoController] {clickedChar.Name} has already moved and cannot be selected!");
                                return;
                            }

                            _selectedCharacter = clickedChar;
                            Debug.Log($"[DemoController] Selected character: {_selectedCharacter.Name} (HP: {_selectedCharacter.CurrentHP}, Speed budget: {_selectedCharacter.CurrentMovementSpeed})");
                            UpdateReachableTileHighlights();
                        }
                    }
                    else if (_selectedCharacter != null)
                    {
                        TryMoveTo(targetPos);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                if (_commandManager.CanUndo)
                {
                    Debug.Log("[DemoController] Undo executed.");
                    _commandManager.Undo();
                    UpdateReachableTileHighlights();
                }
                else
                {
                    Debug.Log("[DemoController] Nothing to Undo!");
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                if (_commandManager.CanRedo)
                {
                    Debug.Log("[DemoController] Redo executed.");
                    _commandManager.Redo();
                    UpdateReachableTileHighlights();
                }
                else
                {
                    Debug.Log("[DemoController] Nothing to Redo!");
                }
            }

            if (TryGetTerrainFromNumericInput(out TerrainType newTerrain))
            {
                GridPosition hoverPos = GetHoveredGridPosition();
                if (_gridView.GridModel.IsValidPosition(hoverPos))
                {
                    ITile tile = _gridView.GridModel.GetTile(hoverPos);
                    if (tile != null && tile.CurrentTerrain != newTerrain)
                    {
                        var command = new ChangeTerrainCommand(tile, newTerrain);
                        _commandManager.ExecuteCommand(command);
                        Debug.Log($"[DemoController] Changed terrain at {hoverPos} to {newTerrain}. Press U to Undo.");
                        UpdateReachableTileHighlights();
                    }
                }
            }

            // Melee Attack key: A
            if (Input.GetKeyDown(KeyCode.A))
            {
                if (_selectedCharacter != null)
                {
                    GridPosition hoverPos = GetHoveredGridPosition();
                    if (_gridView.GridModel.IsValidPosition(hoverPos))
                    {
                        ICharacter targetChar = null;
                        foreach (var ch in _characters)
                        {
                            if (ch.Position == hoverPos)
                            {
                                targetChar = ch;
                                break;
                            }
                        }

                        if (targetChar != null && targetChar != _selectedCharacter)
                        {
                            var attackCommand = new MeleeAttackCommand(_selectedCharacter, targetChar);
                            _commandManager.ExecuteCommand(attackCommand);
                            UpdateReachableTileHighlights();
                        }
                        else
                        {
                            Debug.LogWarning("[DemoController] Melee attacks must target another adjacent unit!");
                        }
                    }
                }
            }

            // Elemental spell keys: F(Fire), I(Ice), E(Earth), W(Wind)
            if (Input.GetKeyDown(KeyCode.F))
            {
                CastSpellAtHovered("Fire");
            }
            if (Input.GetKeyDown(KeyCode.I))
            {
                CastSpellAtHovered("Ice");
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                CastSpellAtHovered("Earth");
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                CastSpellAtHovered("Wind");
            }
        }

        /// <summary>
        /// Casts an elemental spell at the hovered tile, modifying terrain and/or character status effects in a 3x3 radius.
        /// </summary>
        private void CastSpellAtHovered(string element)
        {
            GridPosition hoverPos = GetHoveredGridPosition();
            if (_gridView.GridModel.IsValidPosition(hoverPos))
            {
                var command = new CastSpellCommand(hoverPos, element, _gridView.GridModel, CharacterRegistry.AllCharacters);
                _commandManager.ExecuteCommand(command);
                Debug.Log($"[DemoController] Cast {element} spell at {hoverPos}. Press U to Undo.");
                UpdateReachableTileHighlights();
            }
        }

        /// <summary>
        /// Obtains the model GridPosition currently under the mouse cursor.
        /// </summary>
        private GridPosition GetHoveredGridPosition()
        {
            if (Camera.main == null) return new GridPosition(-1, -1);
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector3Int cellPos = _tilemap.WorldToCell(mouseWorldPos);
            cellPos.z = 0;
            BoundsInt bounds = _tilemap.cellBounds;
            return new GridPosition(cellPos.x - bounds.xMin, cellPos.y - bounds.yMin);
        }

        /// <summary>
        /// Evaluates numeric keyboard triggers (1-8 or Keypad 1-8) to specify a matching TerrainType.
        /// </summary>
        private bool TryGetTerrainFromNumericInput(out TerrainType selectedType)
        {
            selectedType = TerrainType.Ground;
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) { selectedType = TerrainType.Ground; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) { selectedType = TerrainType.Grass; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) { selectedType = TerrainType.Tree; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) { selectedType = TerrainType.Fire; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) { selectedType = TerrainType.Rock; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) { selectedType = TerrainType.Mud; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) { selectedType = TerrainType.Ice; return true; }
            if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) { selectedType = TerrainType.Water; return true; }
            return false;
        }

        /// <summary>
        /// Calculates the Dijkstra path and triggers the MoveCharacterCommand on the command manager if target is reachable.
        /// </summary>
        private void TryMoveTo(GridPosition targetPos)
        {
            if (_selectedCharacter == null) return;

            GridPosition startPos = _selectedCharacter.Position;

            if (startPos == targetPos)
            {
                Debug.Log($"[DemoController] {_selectedCharacter.Name} is already standing on this tile!");
                return;
            }

            int budget = (int)_selectedCharacter.CurrentMovementSpeed;
            List<GridPosition> path = _pathfinder.FindPath(_gridView.GridModel, startPos, targetPos, budget);

            if (path != null && path.Count > 0)
            {
                var moveCommand = new MoveCharacterCommand(_selectedCharacter, targetPos, path, _gridView.GridModel);
                _commandManager.ExecuteCommand(moveCommand);
                Debug.Log($"[DemoController] Moved {_selectedCharacter.Name} to {targetPos}.");
            }
            else
            {
                Debug.LogWarning($"[DemoController] Cannot reach tile {targetPos}! Either it's blocked by an obstacle (Rock) or exceeds the movement budget ({budget} cost).");
            }
        }

        /// <summary>
        /// Refreshes colors in the Tilemap: walkable range tiles are set to white, unreachable tiles are darkened.
        /// </summary>
        private void UpdateReachableTileHighlights()
        {
            if (_gridView == null || _gridView.GridModel == null || _tilemap == null) return;

            // If no character is selected, reset all tiles to white (normal colors)
            if (_selectedCharacter == null)
            {
                BoundsInt b = _tilemap.cellBounds;
                int w = _gridView.GridModel.Width;
                int h = _gridView.GridModel.Height;

                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        Vector3Int tilemapCoord = new Vector3Int(x + b.xMin, y + b.yMin, 0);
                        if (_tilemap.HasTile(tilemapCoord))
                        {
                            _tilemap.SetTileFlags(tilemapCoord, TileFlags.None);
                            _tilemap.SetColor(tilemapCoord, Color.white);
                        }
                    }
                }
                return;
            }

            HashSet<GridPosition> reachable = _pathfinder.GetReachableTiles(_gridView.GridModel, _selectedCharacter.Position, (int)_selectedCharacter.CurrentMovementSpeed);

            BoundsInt bounds = _tilemap.cellBounds;
            int width = _gridView.GridModel.Width;
            int height = _gridView.GridModel.Height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridPosition pos = new GridPosition(x, y);
                    Vector3Int tilemapCoord = new Vector3Int(x + bounds.xMin, y + bounds.yMin, 0);

                    if (_tilemap.HasTile(tilemapCoord))
                    {
                        _tilemap.SetTileFlags(tilemapCoord, TileFlags.None);

                        if (reachable.Contains(pos))
                        {
                            _tilemap.SetColor(tilemapCoord, Color.white);
                        }
                        else
                        {
                            _tilemap.SetColor(tilemapCoord, new Color(0.4f, 0.4f, 0.4f, 1f));
                        }
                    }
                }
            }
        }

        private void OpenActionMenu(ICharacter character)
        {
            if (character == null || _actionMenuPanel == null) return;

            _currentInputState = InputState.ActionMenu;

            CharacterView view = GetCharacterView(character);
            if (view != null)
            {
                _actionMenuPanel.SetActive(true);

                // Position the panel near the character's world position projected on screen
                Vector3 screenPos = Camera.main.WorldToScreenPoint(view.transform.position);
                screenPos.x += 100f; // offset to the right
                screenPos.y += 50f;  // offset up
                _actionMenuPanel.transform.position = screenPos;

                // Configure dynamic layout based on unit class type
                if (view.CharacterType == CharacterType.Warrior)
                {
                    if (_attackButton != null) _attackButton.gameObject.SetActive(true);
                    if (_iceButton != null) _iceButton.gameObject.SetActive(false);
                    if (_fireButton != null) _fireButton.gameObject.SetActive(false);
                    if (_earthButton != null) _earthButton.gameObject.SetActive(false);
                    if (_windButton != null) _windButton.gameObject.SetActive(false);

                    // Warrior attack button is interactable only if adjacent enemy exists
                    if (_attackButton != null)
                    {
                        _attackButton.interactable = HasAdjacentEnemy(character);
                    }
                }
                else if (view.CharacterType == CharacterType.Mage)
                {
                    if (_attackButton != null) _attackButton.gameObject.SetActive(false);
                    if (_iceButton != null) _iceButton.gameObject.SetActive(true);
                    if (_fireButton != null) _fireButton.gameObject.SetActive(true);
                    if (_earthButton != null) _earthButton.gameObject.SetActive(true);
                    if (_windButton != null) _windButton.gameObject.SetActive(true);
                }

                if (_waitButton != null) _waitButton.gameObject.SetActive(true);
            }
        }

        private void CloseActionMenu()
        {
            if (_actionMenuPanel != null) _actionMenuPanel.SetActive(false);
            _currentInputState = InputState.Normal;
        }

        private void DeselectCharacter()
        {
            _selectedCharacter = null;
            _gridView.ClearHighlights();
            UpdateReachableTileHighlights();
        }

        private CharacterView GetCharacterView(ICharacter character)
        {
            foreach (var cv in _characterViews)
            {
                if (cv != null && cv.Model == character)
                {
                    return cv;
                }
            }
            return null;
        }

        private bool HasAdjacentEnemy(ICharacter character)
        {
            GridPosition pos = character.Position;
            GridPosition[] adjacentOffsets = {
                new GridPosition(0, 1),
                new GridPosition(0, -1),
                new GridPosition(1, 0),
                new GridPosition(-1, 0)
            };

            foreach (var offset in adjacentOffsets)
            {
                GridPosition targetPos = new GridPosition(pos.X + offset.X, pos.Y + offset.Y);
                if (_gridView.GridModel.IsValidPosition(targetPos))
                {
                    ICharacter neighbor = GetCharacterAt(targetPos);
                    if (neighbor != null && neighbor.Team != character.Team)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private ICharacter GetCharacterAt(GridPosition pos)
        {
            foreach (var ch in _characters)
            {
                if (ch.Position == pos)
                {
                    return ch;
                }
            }
            return null;
        }

        private bool IsAdjacent(GridPosition a, GridPosition b)
        {
            return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y) == 1;
        }

        private void HighlightMeleeTargets()
        {
            _gridView.ClearHighlights();

            GridPosition pos = _selectedCharacter.Position;
            GridPosition[] adjacentOffsets = {
                new GridPosition(0, 1),
                new GridPosition(0, -1),
                new GridPosition(1, 0),
                new GridPosition(-1, 0)
            };

            foreach (var offset in adjacentOffsets)
            {
                GridPosition targetPos = new GridPosition(pos.X + offset.X, pos.Y + offset.Y);
                if (_gridView.GridModel.IsValidPosition(targetPos))
                {
                    ICharacter enemy = GetCharacterAt(targetPos);
                    if (enemy != null && enemy.Team != _selectedCharacter.Team)
                    {
                        _gridView.HighlightTile(targetPos, new Color(1f, 0f, 0f, 0.4f)); // red highlight
                    }
                }
            }
        }

        private void HighlightSpellTargets()
        {
            _gridView.ClearHighlights();

            int width = _gridView.GridModel.Width;
            int height = _gridView.GridModel.Height;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridPosition pos = new GridPosition(x, y);
                    if (_gridView.GridModel.IsValidPosition(pos))
                    {
                        _gridView.HighlightTile(pos, new Color(0.2f, 0.6f, 1f, 0.2f)); // light blue highlight
                    }
                }
            }
        }

        private void HandleWaitClicked()
        {
            if (_selectedCharacter == null) return;

            Debug.Log($"[DemoController] Wait selected for {_selectedCharacter.Name}. Ending their turn.");
            if (_selectedCharacter.CurrentMovementSpeed > 0f)
            {
                var waitCommand = new ModifyMovementSpeedCommand(_selectedCharacter, -_selectedCharacter.CurrentMovementSpeed);
                _commandManager.ExecuteCommand(waitCommand);
            }

            CloseActionMenu();
            DeselectCharacter();
        }

        private void HandleAttackClicked()
        {
            if (_selectedCharacter == null) return;

            Debug.Log($"[DemoController] Melee Attack selected for {_selectedCharacter.Name}. Choose adjacent target.");
            if (_actionMenuPanel != null) _actionMenuPanel.SetActive(false);
            _currentInputState = InputState.TargetingAttack;

            HighlightMeleeTargets();
        }

        private void HandleSpellClicked(string spellName)
        {
            if (_selectedCharacter == null) return;

            Debug.Log($"[DemoController] Spell '{spellName}' selected for {_selectedCharacter.Name}. Choose target tile.");
            if (_actionMenuPanel != null) _actionMenuPanel.SetActive(false);
            _currentInputState = InputState.TargetingSpell;
            _pendingSpellName = spellName;

            HighlightSpellTargets();
        }

        /// <summary>
        /// Generates a procedural 32x32 white square sprite with a black border to use as a fallback token.
        /// </summary>
        private Sprite CreateDefaultSprite()
        {
            Texture2D texture = new Texture2D(32, 32);
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    if (x == 0 || x == 31 || y == 0 || y == 31)
                        texture.SetPixel(x, y, Color.black);
                    else
                        texture.SetPixel(x, y, Color.white);
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
