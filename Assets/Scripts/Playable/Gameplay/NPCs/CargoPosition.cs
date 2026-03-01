using UnityEngine;

namespace Playable.Gameplay.NPCs
{
    public class CargoPosition : MonoBehaviour
    {
        public Animator Animator;
        [SerializeField] private Transform _refBone;
        [SerializeField] private float _yOffset;
        
        private static readonly int Hash_HandHold = Animator.StringToHash("HandHold");

        private void Update()
        {
            if (Animator.GetBool(Hash_HandHold))
            {
                var pos = transform.position;
                transform.position = new Vector3(pos.x, _refBone.position.y + _yOffset, pos.z);
            }
        }
    }
}