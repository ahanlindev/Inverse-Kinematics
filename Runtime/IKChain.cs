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

        [Tooltip("Object that joints on the chain lean towards." +
        "\nNote that in chains with more than 3 total joints," +
        " this can result in odd configurations without caution")]
        public Transform poleTarget; // TODO public or serialized?

        [Tooltip("Omnidirectional restriction on how far each joint can bend, in degrees. "+
        "WARNING: May not be respected if a pole target exists for this chain")]
        [Range(0,180)]public float maxBendAngle = 180f;

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
        private Transform _rootJoint; 

        // index 0 is the position of the root. Length is # of joints
        private List<Transform> _jointTransforms; 
        private List<Vector3> _jointPositions; 

        // index 0 is distance between root and its child in the chain. Length is # of joints - 1
        private List<float> _jointDistances;
        
        // Info about joints before new positions are calculated. Used for rotation. Length is # of joints
        private List<Vector3> _jointStartDirections;
        private List<Quaternion> _jointStartRotations;

        // TODO store target orientation
        // TODO store root rotation separately?
                
        // position and rotation of target on last frame. If these are the same between frames, do not solve
        private Quaternion _targetStartRotation;
        private Quaternion _rootStartRotation;

        private bool _isValid = false; 
    
        private void Awake() {
            _rootJoint = transform;
            CheckValidity(true);
            Init();
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
         * Initializes the list of transforms and distances that form the joints 
         * between this object and the end effector
         * Prerequisite: This IKChain must be valid
         */
         private void Init() {
            if (!_isValid) {return;}
            _jointTransforms = new List<Transform>();
            _jointStartDirections = new List<Vector3>();
            _jointStartRotations = new List<Quaternion>();

            Transform current = endEffector;
            Transform prev = endEffector;
            // traverse up hierarchy to the root of the chain
            while(current != null && !current.Equals(_rootJoint.parent)) {
                _jointTransforms.Add(current);
                _jointStartRotations.Add(current.rotation);
                if (current.Equals(endEffector)) {
                    _jointStartDirections.Add((target.position - current.position).normalized);
                } else {
                    _jointStartDirections.Add((prev.position - current.position).normalized);
                }

                prev = current;
                current = current.parent;
            }
            // loop puts end effector at index 0. Algorithm is easier if root is at 0
            _jointTransforms.Reverse();
            _jointStartDirections.Reverse();
            _jointStartRotations.Reverse();

            _targetStartRotation = target.rotation;
            _rootStartRotation = (_rootJoint.parent != null) ? _rootJoint.parent.rotation : Quaternion.identity;
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

            SetPositionsAndRotations();
        }   
        
        /**
         * Updates the lists of information necessary for each frame
         */
        private void UpdateLists() {
            _jointDistances = new List<float>(); // clear lists and rebuild them
            _jointPositions = new List<Vector3>();
            for(int i = 0; i < _jointTransforms.Count; i++) {
                Transform current = _jointTransforms[i];
                _jointPositions.Add(current.position);

                if (i < _jointTransforms.Count - 1) {
                    Transform next = _jointTransforms[i+1];

                    float dist = Vector3.Distance(current.position, next.position);
                    _jointDistances.Add(dist);
                }
            }
        }
        /**
         * Set each joint at the appropriate distance along the line between the root and 
         * the target
         * NOTE: as of now this is unused, because I have not found a good way to get 
         */
        private void HandleUnreachableTarget() {
            //TODO I should probably impose the joint restraint on the root here 
            for(int i = 0; i < _jointDistances.Count; i++) {
                Vector3 current = _jointPositions[i];

                // get distance between target and joint i
                float dist = Vector3.Distance(target.position, current);
                float tval = _jointDistances[i] / dist;

                // lerp position of (i+1)th joint based on distance
                Vector3 newPos = Vector3.Lerp(current, target.position, tval);

                // knownDir is axis formed by joints earlier in the chain. Special case when i = 0
                Vector3 knownDir = (i > 0) ? (current - _jointPositions[i-1]).normalized : _jointStartDirections[0];
                Vector3 newDir = (newPos - current).normalized;

                // check that angle between directions is within maxBendAngle 
                float bendAngle = Vector3.Angle(knownDir, newDir); 
                if (bendAngle > maxBendAngle) {
                    float difference = bendAngle - maxBendAngle;
                    Vector3 fixedDir = Vector3.RotateTowards(newDir, knownDir, Mathf.Deg2Rad * difference, 0f);
                    newPos = current + (_jointDistances[i] * fixedDir);
                }

                _jointPositions[i+1] = newPos;
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
                _jointPositions[_jointPositions.Count -1] = target.position;
                // iterate down the chain and set each position to a reasonable location
                for (int i = _jointDistances.Count - 1; i >= 0; i--) {
                    Vector3 current = _jointPositions[i];
                    Vector3 next = _jointPositions[i + 1];
                    
                    // get the distance between current and next at this moment
                    float tempDist = Vector3.Distance(next, current);

                    // lerp current to the appropriate distance away from the next joint
                    float tval = _jointDistances[i] / tempDist;
                    Vector3 newPos = Vector3.LerpUnclamped(next, current, tval);

                    _jointPositions[i] = newPos;
                }

                // BACKWARD
                // Set root position to start
                _jointPositions[0] = startingRootPos;
                // iterate up the chain and set each position to a reasonable location
                for (int i = 0; i < _jointDistances.Count; i++) {
                    Vector3 current = _jointPositions[i];
                    Vector3 next = _jointPositions[i + 1];

                    // get the distance between current and next at this moment
                    float tempDist = Vector3.Distance(current, next);

                    // lerp next to the appropriate distance away from the current joint
                    float tval = _jointDistances[i] / tempDist;
                    Vector3 newPos = Vector3.LerpUnclamped(current, next, tval);

                    // Check restraints on bending
                    // get vectors on lines formed by adjacent joints.
                    // knownDir is axis formed by joints earlier in the chain. Special case when i = 0
                    Vector3 knownDir = (i > 0) ? (current - _jointPositions[i-1]).normalized : _jointStartDirections[0];
                    Vector3 newDir = (newPos - current).normalized;

                    // check that angle between directions is within maxBendAngle 
                    float bendAngle = Vector3.Angle(knownDir, newDir); 
                    if (bendAngle > maxBendAngle) {
                        float difference = bendAngle - maxBendAngle;
                        Vector3 fixedDir = Vector3.RotateTowards(newDir, knownDir, Mathf.Deg2Rad * difference, 0f);
                        newPos = current + (_jointDistances[i] * fixedDir);
                    }

                    _jointPositions[i + 1] = newPos;
                }
            }
        }

        /**
         * Adjusts each joint in the chain so that it bends towards the pole target. This makes animation more consistent
          * TODO Should I just use ee and root as the axis?
         */
        private void HandlePoleConstraint() {
            // loop condition here only respects pole if there are three or more joints
            for(int i = 1; i < _jointTransforms.Count - 1; i++) {
                Vector3 prev = _jointPositions[i-1];
                Vector3 current = _jointPositions[i];
                Vector3 next = _jointPositions[i+1];

                // Project the pole target onto the plane that is orthogonal to the axis
                // formed by prev and next, and intersects current
                Vector3 norm = (next - prev).normalized;
                Vector3 vecToTgt = poleTarget.position - current;
                Vector3 projectedTgt = current + Vector3.ProjectOnPlane(vecToTgt, norm);
                
                // project prev onto the plane to get the center of the circle that current can 
                // theoretically rotate around
                Vector3 vecToPrev = prev - current;
                Vector3 circleOrigin = current + Vector3.ProjectOnPlane(vecToPrev, norm);

                // Get ratio of moveable radius to distance from tgt
                float radius = Vector3.Distance(circleOrigin, current);
                float poleDist = Vector3.Distance(circleOrigin, projectedTgt);
                float tVal = radius/poleDist;

                // Update current position to be most reasonably close to the pole
                Vector3 poleOrientedPos = Vector3.LerpUnclamped(circleOrigin, projectedTgt, tVal);
                _jointPositions[i] = poleOrientedPos;
            }       
        }
 
        // Updates transform positions with the positions calculated by the algorithm,
        // and sets rotation of objects to be reasonable to the positional changes 
        private void SetPositionsAndRotations() {
            for(int i = 0; i < _jointPositions.Count - 1; i++) {
                Vector3 current = _jointPositions[i];
                Vector3 next = _jointPositions[i + 1];
                Vector3 oldDir = _jointStartDirections[i];
                Vector3 newDir = (next - current).normalized;

                Quaternion newRot = Quaternion.FromToRotation(oldDir, newDir);

                _jointTransforms[i].rotation = newRot * _jointStartRotations[i];
            }
            // THIS IS AN ASSUMPTION. TODO find a more graceful way
            endEffector.rotation = _jointTransforms[_jointTransforms.Count - 1].rotation;

            for(int i = 0; i < _jointPositions.Count; i++) {
                _jointTransforms[i].position = _jointPositions[i];
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
