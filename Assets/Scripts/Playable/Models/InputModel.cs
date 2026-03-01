using UnityEngine;
using Utility;

namespace Playable.Models
{
    public class InputModel
    {
        public Vector2 MovementVector;
        public readonly SourceTracker CharacterControlBlocker = new SourceTracker();
        public readonly SourceTracker PlacementPaymentBlocker = new SourceTracker();
    }
}