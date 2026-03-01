using UnityEngine;

namespace Playable.Gameplay.Character
{
    public class CharacterController : MonoBehaviour
    {
        [SerializeField] private float _raycastHeight = 0.1f;
        [SerializeField] private float _raycastDistance = 0.1f;
        [SerializeField] private float _raycastRadius = 0.2f;
        [SerializeField] private float _skinWidth = 0.1f;
        [SerializeField] private float _raycastForwardOffset = -0.5f;
        [SerializeField] private LayerMask _raycastLayerMask;
        [SerializeField] private int _raycastIterations = 2;
        [SerializeField] private float _maxSubstepDistance = 0.1f;
        [SerializeField] private bool _useMultipleRaycasts;
        
        public void Move(Vector3 move, Vector3 moveDir)
        {
            var remainingMove = move;
            var newPos = transform.position;
            var totalMoveDistance = move.magnitude;
            
            // Limit maximum distance per frame to prevent tunneling
            if (totalMoveDistance > _maxSubstepDistance)
            {
                var steps = Mathf.CeilToInt(totalMoveDistance / _maxSubstepDistance);
                var stepMove = move / steps;
                
                for (int step = 0; step < steps; step++)
                    PerformSafeMove(stepMove, ref newPos);
            }
            else
            {
                PerformSafeMove(remainingMove, ref newPos);
            }
            
            newPos.y = transform.position.y;
            transform.position = newPos;
        }
        
        private void PerformSafeMove(Vector3 move, ref Vector3 position)
        {
            var remainingMove = move;
            var iterations = 0;

            Vector3 currentMoveDir;
            Vector3 origin;
            float closestDistance;
            RaycastHit closestHit;
            float castDistance;
            bool hitDetected;
            
            while (iterations < _raycastIterations && remainingMove.sqrMagnitude > Mathf.Epsilon)
            {
                currentMoveDir = remainingMove.normalized;
                origin = position + Vector3.up * _raycastHeight + currentMoveDir * (_raycastRadius * _raycastForwardOffset);
                castDistance = remainingMove.magnitude + _raycastDistance;
                
                hitDetected = false;
                closestHit = default;
                closestDistance = float.MaxValue;
                
                // Primary collision check
                if (Physics.SphereCast(origin, _raycastRadius, currentMoveDir, out RaycastHit hit, castDistance, _raycastLayerMask, QueryTriggerInteraction.Ignore))
                {
                    hitDetected = true;
                    closestHit = hit;
                    closestDistance = hit.distance;
                }
                
                // Additional checks for improved accuracy
                if (_useMultipleRaycasts)
                {
                    // Check with small left and right offsets
                    var perpendicular = Vector3.Cross(currentMoveDir, Vector3.up).normalized;
                    var leftOrigin = origin + perpendicular * (_raycastRadius * 0.5f);
                    var rightOrigin = origin - perpendicular * (_raycastRadius * 0.5f);
                    
                    if (Physics.SphereCast(leftOrigin, _raycastRadius * 0.8f, currentMoveDir, out RaycastHit leftHit, castDistance, _raycastLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        if (!hitDetected || leftHit.distance < closestDistance)
                        {
                            hitDetected = true;
                            closestHit = leftHit;
                            closestDistance = leftHit.distance;
                        }
                    }
                    
                    if (Physics.SphereCast(rightOrigin, _raycastRadius * 0.8f, currentMoveDir, out RaycastHit rightHit, castDistance, _raycastLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        if (!hitDetected || rightHit.distance < closestDistance)
                        {
                            hitDetected = true;
                            closestHit = rightHit;
                            closestDistance = rightHit.distance;
                        }
                    }
                }
                
                if (hitDetected)
                {
                    // Calculate safe distance with additional margin
                    var safeDistance = Mathf.Max(0f, closestHit.distance - _raycastDistance - _skinWidth);
                    var safeMove = currentMoveDir * safeDistance;
                    
                    // Ensure safe movement is not too small
                    if (safeDistance > 0.001f)
                    {
                        position += safeMove;
                        remainingMove -= safeMove;
                    }
                    
                    // Project remaining movement onto obstacle plane
                    var projectedMove = Vector3.ProjectOnPlane(remainingMove, closestHit.normal);
                    
                    // Check that projection doesn't lead back into the wall
                    if (Vector3.Dot(projectedMove.normalized, closestHit.normal) < -0.01f)
                    {
                        // If projection leads into wall, stop
                        break;
                    }
                    
                    remainingMove = projectedMove;
                    
                    // If remaining movement is too small or unchanged, break
                    if (remainingMove.sqrMagnitude < 0.0001f)
                        break;
                }
                else
                {
                    position += remainingMove;
                    break;
                }
                
                iterations++;
            }
        }
        
        private void OnDrawGizmos()
        {
            var origin = transform.position + Vector3.up * _raycastHeight + transform.forward * (_raycastRadius * _raycastForwardOffset);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(origin, _raycastRadius);
            Gizmos.DrawLine(origin, origin + transform.forward * _raycastDistance);
            
            if (_useMultipleRaycasts)
            {
                var perpendicular = Vector3.Cross(transform.forward, Vector3.up).normalized;
                var leftOrigin = origin + perpendicular * (_raycastRadius * 0.5f);
                var rightOrigin = origin - perpendicular * (_raycastRadius * 0.5f);
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(leftOrigin, _raycastRadius * 0.8f);
                Gizmos.DrawWireSphere(rightOrigin, _raycastRadius * 0.8f);
            }
        }
    }
}