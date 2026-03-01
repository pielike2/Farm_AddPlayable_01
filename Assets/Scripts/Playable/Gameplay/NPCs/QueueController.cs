using System;
using System.Collections.Generic;
using System.Linq;
using Base.PoolingSystem;
using UnityEngine;
using Utility;

namespace Playable.Gameplay.NPCs
{
    public class QueueController : MonoBehaviour
    {
        [SerializeField] private LinePath _path;
        [SerializeField] private QueueCharacter _characterPrefab;
        [SerializeField] private Transform _waitPoint;
        [SerializeField] private Transform _shopPoint;
        [SerializeField] private int _minQueueCharactersCount = 5;
        [SerializeField] private float _startDistance;
        [SerializeField] private float _distInterval = 1f;
        [SerializeField] private float _queueContinueDelay = 0.5f;
        [SerializeField] private float _startPurchaseDelay = 0.25f;
        [SerializeField] private StartSpeedMultSettings _startSpeedMultSettings;

        [Serializable]
        public class StartSpeedMultSettings
        {
            public bool isEnabled;
            public float duration = 1f;
            public float speedMult = 1.5f;
        }

        private readonly List<QueueCharacter> _characters = new List<QueueCharacter>();
        private readonly List<QueueCharacter> _activeQueueCharacters = new List<QueueCharacter>();
        
        private float _waitPointDist;
        private float _shopPointDist;
        private int _skinCounter;
        private float _queueContinueDelayEnd;
        private bool _startSpeedMultActivated;
        private float _startSpeedMultEndTime;
        
        public bool PurchaseActive { get; private set; }
        public QueueCharacter BuyerCharacter { get; private set; }

        public event Action<QueueCharacter> OnCharacterSpawn; 

        private void Awake()
        {
            if (_path == null || _waitPoint == null || _shopPoint == null)
                return;

            _path.TryGetPoint(_path.TransformToPointIndex(_waitPoint), out _, out _waitPointDist, out var _);
            _path.TryGetPoint(_path.TransformToPointIndex(_shopPoint), out _, out _shopPointDist, out var _);
        }

        private void Update()
        {
            if (_path == null || _characterPrefab == null)
                return;

            var newCharactersCount = _minQueueCharactersCount - _activeQueueCharacters.Count;
            for (int i = 0; i < newCharactersCount; i++)
                SpawnNewCharacter();
            
            CalculateCharacterMovements();

            UpdatePurchase();

            QueueCharacter character;
            
            for (var i = 0; i < _characters.Count; i++)
            {
                character = _characters[i];
                
                if (_startSpeedMultActivated)
                {
                    character.MovementSpeedMult = Time.time < _startSpeedMultEndTime
                        ? _startSpeedMultSettings.speedMult
                        : 1f;
                }
                
                character.UpdateMovement();
                
                if (character.CurrentPoint == _path.PointsCount - 1)
                {
                    character.Release();
                    _characters.Remove(character);
                    i--;
                }
            }
        }

        private void CalculateCharacterMovements()
        {
            for (int i = 0; i < _activeQueueCharacters.Count; i++)
            {
                if (i >= 2)
                    _activeQueueCharacters[i].ForceSetDistance(_activeQueueCharacters[i - 1].CurrentDistance - _distInterval);
                
                if (Time.time < _queueContinueDelayEnd)
                    _activeQueueCharacters[i].ForceStop(true);
                else
                    _activeQueueCharacters[i].ForceStop(i >= 2);
            }
        }

        private void UpdatePurchase()
        {
            if (_activeQueueCharacters.Count == 0)
                return;
            
            BuyerCharacter = _activeQueueCharacters[0];
                
            if (!BuyerCharacter.Cargo.IsFull && BuyerCharacter.IsOnDestinationPoint)
            {
                BuyerCharacter.QueueState = NpcQueueState.Buying;
                if (BuyerCharacter.DestinationWaitTime > _startPurchaseDelay)
                    PurchaseActive = true;
            }

            if (PurchaseActive && BuyerCharacter.Cargo.IsFull)
            {
                PurchaseActive = false;
                _queueContinueDelayEnd = Time.time + _queueContinueDelay;
            }
            
            if (BuyerCharacter.Cargo.IsFull && BuyerCharacter.IsOnDestinationPoint && Time.time > _queueContinueDelayEnd)
            {
                _activeQueueCharacters.Remove(BuyerCharacter);
                BuyerCharacter.CancelStopAtPoint();
                BuyerCharacter.QueueState = NpcQueueState.Leaving;
                BuyerCharacter = null;
                
                if (_activeQueueCharacters.Count > 0)
                    _activeQueueCharacters[0].StopAtDist(_shopPointDist, _shopPoint.forward);
                if (_activeQueueCharacters.Count > 1)
                    _activeQueueCharacters[1].StopAtDist(_waitPointDist, _waitPoint.forward);
            }
        }

        private void SpawnNewCharacter()
        {
            TryActivateStartSpeedMult();
            
            _path.TryGetPoint(0, out var pos, out var nextDistance, out var nextDir);
            var character = _characterPrefab.Spawn(pos, Quaternion.LookRotation(nextDir, Vector3.up));
            character.SetPath(_path);
            
            if (_characters.Count > 0)
                character.ForceSetDistance(_characters.Last().CurrentDistance - _distInterval);
            else
                character.ForceSetDistance(_startDistance);
                
            _characters.Add(character);
            _activeQueueCharacters.Add(character);
            
            switch (_activeQueueCharacters.Count)
            {
                case 1:
                    character.StopAtDist(_shopPointDist, _shopPoint.forward);
                    break;
                case 2:
                    character.StopAtDist(_waitPointDist, _waitPoint.forward);
                    break;
            }
            
            var skinSwitcher = character.GetComponent<SimpleSkinSwitcher>();
            if (skinSwitcher != null)
            {
                skinSwitcher.SetSkin(_skinCounter % skinSwitcher.SkinsCount);
                _skinCounter++;
            }

            OnCharacterSpawn?.Invoke(character);
        }

        private void TryActivateStartSpeedMult()
        {
            if (!_startSpeedMultSettings.isEnabled || _startSpeedMultActivated)
                return;
            
            _startSpeedMultActivated = true;
            _startSpeedMultEndTime = Time.time + _startSpeedMultSettings.duration;
        }
    }
}