using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ahanlindev
{
    public class IKChain : MonoBehaviour
    {
        // Fields visible in Inspector
        [Tooltip("The end effector of the kinematic chain. MUST BE A DESCENDANT OF THIS GAMEOBJECT")]
        public Transform endEffector; // TODO public or serialized?

        [Tooltip("Object targeted by the end effector")]
        public Transform target; // TODO public or serialized?

        [Tooltip("Object that joints on the chain lean towards."+
        "WARNING: If this chain has more than one joint between the base and end effector, instability may occur")]
        public Transform poleTarget; // TODO public or serialized?

        [Tooltip("If gizmos are enabled, represent the chain as lines between the joints")]
        [SerializeField] private bool _drawChain;

        // TODO put these in an "advanced" dropdown. Will require custom drawer
        [Tooltip("Acceptable distance from target if target is reachable and more iterations of FABRIK are allowed." +
                 " WARNING: If this is too precise, it can cause a crash if there is no cap on iterations")]
        public float tolerance = 0.01f;

        [Tooltip("Maximum number of iterations of FABRIK before finishing.")]
        [SerializeField] private int _maxIterations = 10;
        [Tooltip("If true, joint positions will be updated in FixedUpdate. If false, they will be updated in Update")]
        [SerializeField] private bool _iterateInFixedUpdate = true;

        
        // Transform of this GameObject
        [HideInInspector] private Transform _rootJoint; 

        // index 0 is the position of the root. Length is # of joints
        [HideInInspector] private List<Transform> _jointTransforms; 

        // index 0 is distance between root and its child in the chain. Length is # of joints - 1
        [HideInInspector] private List<float> _jointDistances;
        
        // Direction to the next joint before new positions are calculated. Length is # of joints - 1
        [HideInInspector] private List<Vector3> _jointStartDirections;

        // Initial rotations of each joint before position is solved. Length is # of joints
        [HideInInspector] private List<Quaternion> _jointStartRotations;
        // TODO store target orientation
        // TODO store root rotation separately?
                
        // marked false if anything is broken
        [HideInInspector] private bool _isValid = false; 
    
        private void Awake() {
            _rootJoint = transform;
            CheckValidity(true);
            initTransformList();
        }

        [ExecuteInEditMode]
        private void OnValidate() {
            // need to ensure validity to draw gizmos TODO move this?
            if (_drawChain && !_isValid) {
                _rootJoint = transform;
                CheckValidity(false);
            }
        }

        private void Update() {
            if (!_iterateInFixedUpdate && _isValid) {
                SolveChain();
            }    
        }

        private void FixedUpdate() {
            if (_iterateInFixedUpdate && _isValid) {
                SolveChain();
            }
        }

        private void OnDrawGizmos() {
            DrawChainGizmo();
        }

        /** 
         * Performs the FABRIK algorithm to solve for locations for each joint. 
         * If the chain is invalid or there is no target, returns immediately.
         * This method is heavily based upon the paper cited in the README.
         */
        private void SolveChain() {
            if (target == null) { return; }

            // Get distance from root and make sure its reachable with sum of joint distances
            float targetDist = Vector3.Distance(_rootJoint.position, target.position);
            float reachableDist = 0;
            
            UpdateLists();
            foreach(float dist in _jointDistances) {
                reachableDist += dist;
            }
            if (reachableDist < targetDist) {
                HandleUnreachableTarget();
            } else {
                HandleReachableTarget();
            }
            if (poleTarget != null) {
                HandlePoleConstraint();
            }
        }   
        /**
         * Updates the lists of information necessary for each frame
         */
        private void UpdateLists() {
            _jointDistances = new List<float>(); // clear lists and rebuild them
            for(int i = 0; i < _jointTransforms.Count - 1; i++) {
                Transform current = _jointTransforms[i];
                Transform next = _jointTransforms[i+1];

                float dist = Vector3.Distance(current.position, next.position);
                _jointDistances.Add(dist);
                Vector3 dir = (next.position - current.position).normalized;
                _jointStartDirections.Add(dir);
                _jointStartRotations.Add(current.rotation);
            }
        }
        /**
         * Set each joint at the appropriate distance along the line between the root and 
         * the target
         */
        private void HandleUnreachableTarget() {
            for(int i = 0; i < _jointDistances.Count; i++) {
                Transform parent = _jointTransforms[i];
                Transform child = _jointTransforms[i+1];
                // get distance between target and joint i
                float dist = Vector3.Distance(target.position, parent.position);
                float tval = _jointDistances[i] / dist;

                // lerp position of (i+1)th joint based on distance
                child.position = Vector3.Lerp(parent.position, target.position, tval);
            }
        }

        /**
         * Iteratively approximates the end effector towards the target point, stopping when
         * it either reaches the threshold or the maximum allowed iterations.
         */
        private void HandleReachableTarget() {
            // save original root position
            Vector3 startingRootPos = _rootJoint.position;
            // If end effector isn't on target, iterate the algorithm
            int iteration = 0;
            while (Vector3.Distance(endEffector.position, target.position) > tolerance && iteration++ < _maxIterations) {
                // FORWARD
                // Set end effector position to target
                endEffector.position = target.position;
                // iterate down the chain and set each position to a reasonable location
                for (int i = _jointDistances.Count - 1; i >= 0; i--) {
                    Transform current = _jointTransforms[i];
                    Transform next = _jointTransforms[i+1];

                    // get the distance between current and next at this moment
                    float tempDist = Vector3.Distance(next.position, current.position);

                    // lerp current to the appropriate distance away from the next joint
                    float tval = _jointDistances[i] / tempDist;

                    current.position = Vector3.LerpUnclamped(next.position, current.position, tval);
                }

                // BACKWARD
                // Set root position to start
                _rootJoint.position = startingRootPos;
                // iterate up the chain and set each position to a reasonable location
                for (int i = 0; i < _jointDistances.Count; i++) {
                    Transform current = _jointTransforms[i];
                    Transform next = _jointTransforms[i+1]; 

                    // get the distance between current and next at this moment
                    float tempDist = Vector3.Distance(current.position, next.position);

                    // lerp next to the appropriate distance away from the current joint
                    float tval = _jointDistances[i] / tempDist;
                    next.position = Vector3.LerpUnclamped(current.position, next.position, tval);
                }
            }
        }

        /**
         * Adjusts each joint in the chain so that it bends towards the pole target. This makes animation more consistent
         */
        private void HandlePoleConstraint() {
            // loop condition here only respects pole if there are three or more joints
            for(int i = 1; i < _jointTransforms.Count - 1; i++) {
                Transform prev = _jointTransforms[i-1];
                Transform current = _jointTransforms[i];
                Transform next = _jointTransforms[i+1];

                // save position of next, since changing position of current will affect this as well
                Vector3 nextInitialPos = next.position;

                // Project the pole target onto the plane that is orthogonal to the axis
                // formed by prev and next, and intersects current
                Vector3 norm = (next.position - prev.position).normalized;
                Vector3 vecToTgt = poleTarget.position - current.position;
                Vector3 projectedTgt = current.position + Vector3.ProjectOnPlane(vecToTgt, norm);
                
                // project prev onto the plane to get the center of the circle that current can 
                // theoretically rotate around
                Vector3 vecToPrev = prev.position - current.position;
                Vector3 circleOrigin = current.position + Vector3.ProjectOnPlane(vecToPrev, norm);

                // Get ratio of moveable radius to distance from tgt
                float radius = Vector3.Distance(circleOrigin, current.position);
                float poleDist = Vector3.Distance(circleOrigin, projectedTgt);
                float tVal = radius/poleDist;

                // Update current position to be most reasonably close to the pole
                Vector3 poleOrientedPos = Vector3.LerpUnclamped(circleOrigin, projectedTgt, tVal);
                current.position = poleOrientedPos;
                next.position = nextInitialPos;
            }
            
        }
        // TODO take starting position, ending position, quat between them, and rotate each joint transform by that quat
        /**
         * Checks that each field of this object contains valid data
         * @param printErr: Print out an error message if invalid
         */
        private bool CheckValidity(bool printErr) {
            bool MISSING_END_EFFECTOR = endEffector == null;
            bool NON_DESCENDANT_EE = !(MISSING_END_EFFECTOR);
            
            if (!MISSING_END_EFFECTOR) {
                Transform current = endEffector;
                while (current != null && !current.Equals(transform))
                {
                    current = current.parent;
                    if (transform.Equals(current))
                    {
                        NON_DESCENDANT_EE = false;
                    }
                }
            }

            _isValid = !(MISSING_END_EFFECTOR || NON_DESCENDANT_EE);
            if (!_isValid && printErr)
            {
                Debug.LogWarning("Invalid Kinematic Chain. " + 
                    (MISSING_END_EFFECTOR ? "- Missing End Effector" : "") +
                    (NON_DESCENDANT_EE ? "- End Effector is not a descendant of Start Bone" : "")
                );
            } 
            return _isValid;
        }
        
        /**
         * Initializes the list of transforms and distances that form the joints 
         * between this object and the end effector
         * Prerequisite: This IKChain must be valid
         */
         private void initTransformList() {
            if (!_isValid) {return;}
            _jointTransforms = new List<Transform>();
            Transform current = endEffector;
            Transform prev = endEffector;
            // traverse up hierarchy to the root of the chain
            while(current != null && !current.Equals(_rootJoint.parent)) {
                _jointTransforms.Add(current);
                prev = current;
                current = current.parent;
            }
            // loop puts end effector at index 0. Algorithm is easier if root is at 0
            _jointTransforms.Reverse();
        }

        /** 
         * Draws a magenta line between the joints of this kinematic chain
         * MUST BE CALLED IN OnDrawGizmos or OnDrawGizmosSelected!
         */
        private void DrawChainGizmo() {
            if (_drawChain) {
                if (_isValid) {
                    Transform last = endEffector.transform;
                    Transform current = last;
                    while (current != _rootJoint.transform)
                    {
                        current = last.parent.transform;
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawLine(last.transform.position, current.transform.position);
                        last = current;
                    }
                } else {
                    Debug.LogError("Cannot draw gizmos on this chain because it is not valid");
                    _drawChain = false;
                }
            }
        }
    }
}
