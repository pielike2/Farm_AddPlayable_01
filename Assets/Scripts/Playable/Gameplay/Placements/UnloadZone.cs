using System.Collections;
using System.Collections.Generic;
using Base;
using Base.PoolingSystem;
using Playable.Animations;
using Playable.Gameplay.Character;
using Playable.Gameplay.Loot;
using UnityEngine;

namespace Playable.Gameplay.Placements
{
    public class UnloadZone : MonoBehaviour
    {
        [SerializeField] private Placement _placement;
        [SerializeField] private BaseCargo _targetCargo;
        [SerializeField] private float _interval = 0.1f;
        [SerializeField] private BaseSimpleTweenAnimation _itemDisappearAnim;
        [SerializeField] private BaseSimpleTweenAnimation _itemAppearAnim;
        [SerializeField] private AudioClipShell _putSfx;
        [SerializeField] private bool _unloadFromBottom;

        private Coroutine _unloadCoroutine;
        private WaitForSeconds _intervalWait;
        private bool _isTargetCargoValid;
        
        private List<BaseCargo> _cargos = new List<BaseCargo>();

        private void Reset()
        {
            _placement = GetComponent<Placement>();
        }

        private void Awake()
        {
            _placement.OnStartInteraction += OnStartInteraction;
            _placement.OnStopInteraction += OnStopInteraction;
            
            _intervalWait = new WaitForSeconds(_interval);
            _isTargetCargoValid = _targetCargo != null;
        }

        private void OnStartInteraction(Placement placement, IInteractor interactor)
        {
            if (!_isTargetCargoValid)
                return;
            
            var inventory = interactor.gameObject.GetComponent<CharacterInventory>();
            
            if (inventory == null) 
                return;

            if (inventory.TryGetCargoByItemId(_targetCargo.ItemTypeId, out var sourceCargo))
            {
                if(!_cargos.Contains(sourceCargo))
                    _cargos.Add(sourceCargo);

                if (_unloadCoroutine == null)
                    _unloadCoroutine = StartCoroutine(UnloadRoutine());
            }
        }

        private void OnStopInteraction(Placement placement, IInteractor interactor)
        {
            var inventory = interactor.gameObject.GetComponent<CharacterInventory>();
            if (inventory.TryGetCargoByItemId(_targetCargo.ItemTypeId, out var sourceCargo))
            {
                _cargos.Remove(sourceCargo);
            }

            if (_unloadCoroutine != null && _cargos.Count == 0)
            {
                StopCoroutine(_unloadCoroutine);
                _unloadCoroutine = null;
            }
        }

        private IEnumerator UnloadRoutine()
        {
            var hasResources = true;
            
            while (hasResources)
            {
                if (_cargos.Count == 0)
                {
                    _unloadCoroutine = null;
                    yield break;
                }
                
                hasResources = false;
                
                for (int i = 0; i < _cargos.Count; i++)
                {
                    if (_cargos[i].OccupiedSlotsCount == 0)
                        continue;
                    
                    hasResources = true;
                    
                    if (_unloadFromBottom)
                    {
                        if (_cargos[i].TryReleaseBottomItem(out var item))
                        {
                            HandleUnloadedItem(item);
                            _cargos[i].AnimateFall();
                        }
                    }
                    else
                    {
                        if (_cargos[i].TryReleaseTopItem(out var item, out _))
                            HandleUnloadedItem(item);
                    }
                }
                
                yield return _intervalWait;
            }

            _unloadCoroutine = null;
        }

        private void HandleUnloadedItem(Transform unloadedItem)
        {
            var unloadedLootItem = unloadedItem.GetComponent<ILootItem>();
            if (unloadedLootItem == null)
                return;

            _targetCargo.ReleaseItemFromSlot(unloadedItem);
            
            var seq = _itemDisappearAnim.Animate(unloadedLootItem.transform);
            seq.onComplete = unloadedLootItem.Release;

            if (_targetCargo.IsFull)
                return;
            
            var newItem = unloadedLootItem.NextSourcePrefab.Spawn();
            newItem.ToggleRigidbody(false);
            var slot = _targetCargo.OccupyNextSlot(newItem.transform);
            _targetCargo.PutTransformIntoSlot(newItem.transform, slot);
            _itemAppearAnim.Animate(newItem.transform);
            
            _putSfx.Play();
        }
    }
}