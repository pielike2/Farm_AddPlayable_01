using UnityEngine;
using Utility;
using Utility.SensorSystem;

namespace Playable.Gameplay.Placements
{
    public class PlacementHighlightActivator : MonoBehaviour
    {
        [SerializeField] private BaseSensor _sensor;
        [SerializeField] private HashId _highlightSenseId;

        private SensorFilter _sensorFilter;

        private void Awake()
        {
            _sensorFilter = _sensor.GetFilter(_highlightSenseId);
            
            _sensorFilter.OnAddTarget += target =>
            {
                var highlight = target as IPlacementHighlight;
                highlight?.ToggleHighlight(true);
            };
            
            _sensorFilter.OnRemoveTarget += target =>
            {
                var highlight = target as IPlacementHighlight;
                highlight?.ToggleHighlight(false);
            };
        }
    }
}