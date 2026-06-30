using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Core.Model;

namespace View
{
    public class CharacterView : MonoBehaviour
    {
        private ICharacter _model;
        private GridView _gridView;
        private Tilemap _tilemap;

        private readonly Queue<Vector3> _waypointQueue = new Queue<Vector3>();
        private Vector3 _currentTargetPosition;
        private Vector3 _lastEnqueuedPosition;

        [Header("Movement Animation")]
        [SerializeField] private float _moveSpeed = 6f;

        [Header("Character Base Configuration")]
        [SerializeField] private CharacterType _characterType = CharacterType.Warrior;
        [SerializeField] private Team _team = Team.Player;
        [SerializeField] private int _maxHP = 100;
        [SerializeField] private int _movementBudget = 5;

        public string CharacterName => gameObject.name.Replace("(Clone)", "").Trim();
        public CharacterType CharacterType => _characterType;
        public Team Team => _team;
        public int MaxHP => _maxHP;
        public int MovementBudget => _movementBudget;
        public ICharacter Model => _model;

        public bool IsMoving => _waypointQueue.Count > 0 || Vector3.Distance(transform.position, _currentTargetPosition) > 0.02f;

        /// <summary>
        /// Initializes the CharacterView with the character model, grid view, and tilemap.
        /// </summary>
        public void Initialize(ICharacter model, GridView gridView, Tilemap tilemap)
        {
            _model = model;
            _gridView = gridView;
            _tilemap = tilemap;

            Vector3 startPos = GetWorldPosition(_model.Position);
            transform.position = startPos;
            _currentTargetPosition = startPos;
            _lastEnqueuedPosition = startPos;

            _model.OnCharacterChanged += HandleCharacterChanged;
            HandleCharacterChanged(_model);
        }

        /// <summary>
        /// Updates the character's position, moving smoothly along the waypoint queue.
        /// </summary>
        private void Update()
        {
            if (Vector3.Distance(transform.position, _currentTargetPosition) < 0.02f)
            {
                transform.position = _currentTargetPosition;

                if (_waypointQueue.Count > 0)
                {
                    _currentTargetPosition = _waypointQueue.Dequeue();
                }
            }

            transform.position = Vector3.MoveTowards(transform.position, _currentTargetPosition, _moveSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Responds to changes in the character model, enqueuing new positions and logging current status.
        /// </summary>
        private void HandleCharacterChanged(ICharacter character)
        {
            Vector3 stepPos = GetWorldPosition(character.Position);
            if (Vector3.Distance(stepPos, _lastEnqueuedPosition) > 0.01f)
            {
                _waypointQueue.Enqueue(stepPos);
                _lastEnqueuedPosition = stepPos;
            }

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                if (character.CurrentMovementSpeed <= 0f)
                {
                    spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }
                else
                {
                    spriteRenderer.color = Color.white;
                }
            }

            string statusStr = "";
            if (character.HasStatus("Fire")) statusStr += "[Fire] ";
            if (character.HasStatus("Ice")) statusStr += "[Ice] ";
            if (character.HasStatus("Earth")) statusStr += "[Earth] ";
            if (character.HasStatus("Wind")) statusStr += "[Wind] ";
            if (character.HasStatus("Freeze")) statusStr += "[Freeze] ";
            if (character.HasStatus("Blur")) statusStr += "[Blur] ";
            if (character.HasStatus("ExtinctionSpeedBoost")) statusStr += "[Extinction Speed Boost] ";
            if (character.HasStatus("BurstEvasionBoost")) statusStr += "[Burst Evasion Boost] ";
            if (string.IsNullOrEmpty(statusStr)) statusStr = "None";

            Debug.Log($"[CharacterView] {character.Name} - HP: {character.CurrentHP}/{character.MaxHP} | Evasion: +{character.EvasionModifier} | Speed: {character.CurrentMovementSpeed} | Statuses: {statusStr}");
        }

        /// <summary>
        /// Calculates the world position corresponding to the given grid position.
        /// </summary>
        private Vector3 GetWorldPosition(GridPosition gridPos)
        {
            BoundsInt bounds = _tilemap.cellBounds;
            Vector3Int tilemapCoord = new Vector3Int(gridPos.X + bounds.xMin, gridPos.Y + bounds.yMin, 0);
            return _tilemap.GetCellCenterWorld(tilemapCoord);
        }

        /// <summary>
        /// Unsubscribes from character model events to prevent memory leaks.
        /// </summary>
        private void OnDestroy()
        {
            if (_model != null)
            {
                _model.OnCharacterChanged -= HandleCharacterChanged;
            }
        }
    }
}
