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
        "If a joint along the chain has an IKJoint component, its constraints are used instead" +
        "WARNING: May not be respected if a pole target exists for this chain")] // TODO can I fix that?
        [Range(0,180)]public float maxBendAngle = 180f;

        [Tooltip("If gizmos are enabled, represent the chain as lines between the joints")]
        [SerializeField] private bool drawChain;

        // TODO put these in an "advanced" dropdown. Will require custom drawer
        [Tooltip("Acceptable distance from target if target is reachable and more iterations of FABRIK are allowed." +
                 " WARNING: If this is too precise, it can cause a crash if there is no cap on iterations")]
        public float tolerance = 0.01f;

        [Tooltip("Maximum number of iterations of FABRIK before finishing.")]
        [SerializeField] private int maxIterations = 10;
        [Tooltip("If true, joint positions will be updated in FixedUpdate. If false, they will be updated in Update")]
        [SerializeField] private bool iterateInFixedUpdate = true;

        
        // Transform of this GameObject
        private Transform rootJoint; 

        // index 0 is the position of the root. Length is # of joints
        private List<Transform> jointTransforms; 
        private List<Vector3> jointPositions; 
    
        // index 0 is distance between root and its child in the chain. Length is # of joints - 1
        private List<float> jointDistances;
        private float reachableDist;

        // Info about joints before new positions are calculated. Used for rotation. Length is # of joints
        private List<Vector3> jointStartDirections;
        private List<Quaternion> jointStartRotations;

        // Info about joint-specific constraints
        private List<IKJoint> jointConstraintComponents;

        // TODO store target orientation
        // TODO store root rotation separately?
                
        // position and rotation of target on last frame. If these are the same between frames, do not solve
        private Quaternion targetStartRotation;
        private Quaternion rootStartRotation;

        private bool isValid = false; 
    
        private void Awake() {
            rootJoint = transform;
            CheckValidity(true);
            Init();
        }

        [ExecuteInEditMode]
        private void OnValidate() {
            // need to ensure validity to draw gizmos TODO move this?
            if (drawChain && !isValid) {
                rootJoint = transform;
                CheckValidity(false);
            }
        }

        private void Update() {
            if (!iterateInFixedUpdate && isValid) {
                if(rootJoint.parent != null && rootJoint.parent.rotation != rootStartRotation) { // TODO do I need more here
                    rootStartRotation = (rootJoint.parent != null) ? rootJoint.parent.rotation : Quaternion.identity;
                }
                SolveChain();
            }    
        }

        private void FixedUpdate() {
            if (iterateInFixedUpdate && isValid) {
                if(rootJoint.parent != null && rootJoint.parent.rotation != rootStartRotation) { // TODO do I need more here
                    rootStartRotation = (rootJoint.parent != null) ? rootJoint.parent.rotation : Quaternion.identity;
                }
                SolveChain();
            }
        }

        private void OnDrawGizmos() {
            #if UNITY_EDITOR
                DrawChainGizmo();
            #endif
        }

         /**
         * Initializes the list of transforms and distances that form the joints 
         * between this object and the end effector
         * Prerequisite: This IKChain must be valid
         */
         private void Init() {
            if (!isValid) {return;}
            jointTransforms = new List<Transform>();
            jointStartDirections = new List<Vector3>();
            jointStartRotations = new List<Quaternion>();
            jointConstraintComponents = new List<IKJoint>();

            targetStartRotation = target.rotation;
            rootStartRotation = (rootJoint.parent != null) ? rootJoint.parent.rotation : Quaternion.identity;

            Transform current = endEffector;
            Transform prev = endEffector;
            // traverse up hierarchy to the root of the chain
            while(current != null && !current.Equals(rootJoint.parent)) {
                jointTransforms.Add(current);
                
                jointStartRotations.Add(current.rotation);
                
                if (current.Equals(endEffector)) {
                    // TODO incorporate rotation relative to parent?
                    jointStartDirections.Add((target.position - current.position).normalized);
                } else {
                    jointStartDirections.Add((prev.position - current.position).normalized);
                }

                jointConstraintComponents.Add(current.GetComponent<IKJoint>());

                prev = current;
                current = current.parent;
            }
            // loop puts end effector at index 0. Algorithm is easier if root is at 0 TODO this is inefficient
            jointTransforms.Reverse();
            jointStartRotations.Reverse();
            jointStartDirections.Reverse();
            jointConstraintComponents.Reverse();
        }

        /** 
         * Performs the FABRIK algorithm to solve for locations for each joint. 
         * If the chain is invalid or there is no target, returns immediately.
         * This method is heavily based upon the paper cited in the README.
         */
        private void SolveChain() {
            if (target == null) { return; }

            // Get distance from root and make sure its reachable with sum of joint distances
            float targetDist = Vector3.Distance(rootJoint.position, target.position);
            
            // Update data necessary for performing the algorithm
            UpdateLists();
            
            // calculation is faster if unreachable, so check for it first
            if (reachableDist < targetDist) {
                HandleUnreachableTarget();
            } else {
                HandleReachableTarget();
            }

            // Apply favor towards pole target if it exists
            if (poleTarget != null) {
                HandlePoleConstraint(); 
            }

            // Update joint transforms with the calculated positions and rotations
            SetPositionsAndRotations();
        }   
        
        /**
         * Updates the lists of information necessary for each frame
         */
        private void UpdateLists() {
            jointDistances = new List<float>(); // clear lists and rebuild them
            jointPositions = new List<Vector3>();
            for(int i = 0; i < jointTransforms.Count; i++) {
                Transform current = jointTransforms[i];
                jointPositions.Add(current.position);

                if (i < jointTransforms.Count - 1) {
                    Transform next = jointTransforms[i+1];

                    float dist = Vector3.Distance(current.position, next.position);
                    jointDistances.Add(dist);
                    reachableDist += dist;
                }
            }
        }
        /**
         * Set each joint at the appropriate distance along the line between the root and 
         * the target
         * NOTE: as of now this is unused, because I have not found a good way to get 
         */
        private void HandleUnreachableTarget() {
            for(int i = 0; i < jointDistances.Count; i++) {
                Vector3 current = jointPositions[i];

                // get distance between target and joint i
                float dist = Vector3.Distance(target.position, current);
                float tval = jointDistances[i] / dist;

                // lerp position of (i+1)th joint based on distance
                Vector3 newPos = Vector3.Lerp(current, target.position, tval);

                Vector3 constraintDir = GetJointConstraintDirection(i);
                float maxAngle = (jointConstraintComponents[i] == null) ? maxBendAngle : jointConstraintComponents[i].maxAngle;

                Vector3 newDir = (newPos - current).normalized;

                // check that angle between directions is within maxBendAngle 
                float bendAngle = Vector3.Angle(constraintDir, newDir); 
                if (bendAngle > maxAngle) {
                    float difference = bendAngle - maxAngle;
                    Vector3 fixedDir = Vector3.RotateTowards(newDir, constraintDir, Mathf.Deg2Rad * difference, 0f);
                    newPos = current + (jointDistances[i] * fixedDir);
                }

                jointPositions[i+1] = newPos;
            }
        }

        /**
         * Iteratively approximates the end effector towards the target point, stopping when
         * it either reaches the threshold or the maximum allowed iterations.
         */
        private void HandleReachableTarget() {
            // If end effector isn't on target, iterate the algorithm
            int iteration = 0;
            while (Vector3.Distance(endEffector.position, target.position) > tolerance && iteration++ < maxIterations) {
                HandleForward();
                HandleBackward();
            }
        }

        /** 
         * Handles the forward portion of the FABRIK algorithm
         */
        private void HandleForward() {
            // Set end effector position to target
            jointPositions[jointPositions.Count -1] = target.position;
            // iterate down the chain and set each position to a reasonable location
            for (int i = jointDistances.Count - 1; i >= 0; i--) {
                Vector3 current = jointPositions[i];
                Vector3 next = jointPositions[i + 1];
                
                // get the distance between current and next at this moment
                float tempDist = Vector3.Distance(next, current);

                // lerp current to the appropriate distance away from the next joint
                float tval = jointDistances[i] / tempDist;
                Vector3 newPos = Vector3.LerpUnclamped(next, current, tval);

                jointPositions[i] = newPos;
            }
        }

        /**
         * Handles the backward portion of the FABRIK algorithm
         */
        private void HandleBackward() {
            // Set root position to start (transform positions only updated at end of algo)
            jointPositions[0] = rootJoint.position;
            // iterate up the chain and set each position to a reasonable location
            for (int i = 0; i < jointDistances.Count; i++) {
                Vector3 current = jointPositions[i];
                Vector3 next = jointPositions[i + 1];

                // get the distance between current and next at this moment
                float tempDist = Vector3.Distance(current, next);

                // lerp next to the appropriate distance away from the current joint
                float tval = jointDistances[i] / tempDist;
                Vector3 newPos = Vector3.LerpUnclamped(current, next, tval);
                Vector3 newDir = (newPos - current).normalized;
                
                // Account for constraints. If within constraints, nothing will change
                newDir = ConstrainDirection(newDir, i);
                newPos = current + (jointDistances[i] * newDir);
                // Check restraints on bending
                // get reference direction based on line formed by previous "bone". Special case when i=0
                // Vector3 constraintDir = GetJointConstraintDirection(i);
                // float maxAngle = (jointConstraintComponents[i] == null) ? maxBendAngle : jointConstraintComponents[i].maxAngle;

                // Vector3 newDir = (newPos - current).normalized;

                // // check that angle between directions is within maxAngle 
                // float bendAngle = Vector3.Angle(constraintDir, newDir); 
                // if (bendAngle > maxAngle) {
                //     float difference = bendAngle - maxAngle;
                //     Vector3 fixedDir = Vector3.RotateTowards(newDir, constraintDir, Mathf.Deg2Rad * difference, 0f);
                //     newPos = current + (jointDistances[i] * fixedDir);
                // }
                
                jointPositions[i + 1] = newPos;
            }
        }

        /**
         * Returns the normalized vector that represents the nearest direction to dir that is 
         * compatible with the constraints of the joint at the specified index
         */
        private Vector3 ConstrainDirection(Vector3 dir, int index) {
            dir = dir.normalized; // ensures that dir is normalized
            Vector3 constraintDir = GetJointConstraintDirection(index);
            float maxAngle = maxBendAngle; 

            // Max angle is either constraint-dependent, or defaults to the chain's limit
            IKJoint constraint = jointConstraintComponents[index]; 
            if (constraint != null && constraint.isActiveAndEnabled) {
                maxAngle = constraint.maxAngle;  
            } 
            
            // Ensure that the dir is within maxAngle degrees from constraintDir
            float bendAngle = Vector3.Angle(constraintDir, dir);
            if (bendAngle > maxAngle) {
                float diff = bendAngle - maxAngle;
                return Vector3.RotateTowards(dir, constraintDir, Mathf.Deg2Rad * diff, 0f);
            }
            else return dir;
        }

        /**
         * Returns the normalized vector that marks the center of the cone of angular restraint around
         * the joint at the specified index of jointTransforms.
         */
        private Vector3 GetJointConstraintDirection(int index) {
            IKJoint constraint = jointConstraintComponents[index]; 
            if (constraint != null && constraint.isActiveAndEnabled) {
                // If an IKJoint exists, use its values and update them to match current state of the chain
                Quaternion rotFromInitial = Quaternion.identity;
                if (index > 0) {
                    // get direction of the previous joint in the chain
                    Vector3 currentParentDir = (jointPositions[index] - jointPositions[index - 1]).normalized;
                    // get rotation from initial parent direction to current parent direction 
                    rotFromInitial = Quaternion.FromToRotation(jointStartDirections[index - 1], currentParentDir);
                } else {
                    // TODO Figure out how to handle root case if parented
                }
                return rotFromInitial * constraint.GetDirection(); 
            } else if (index > 0) {
                // If there is no IKJoint, assume the constraint is centered on the same direction as the previous "bone"
                return (jointPositions[index] - jointPositions[index - 1]).normalized;
            } else {
                // If there is no previous bone, use the starting direction TODO adjust to work with real-time dir if root is parented
                return jointStartDirections[0];
            }
        }

        /**
         * Adjusts each joint in the chain so that it bends towards the pole target. This makes animation more consistent
          * TODO Should I just use ee and root as the axis?
         */
        private void HandlePoleConstraint() {
            // loop condition here only respects pole if there are three or more joints
            for(int i = 1; i < jointTransforms.Count - 1; i++) {
                Vector3 prev = jointPositions[i-1];
                Vector3 current = jointPositions[i];
                Vector3 next = jointPositions[i+1];

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
                jointPositions[i] = poleOrientedPos;
            }       
        }
 
        // Updates transform positions with the positions calculated by the algorithm,
        // and sets rotation of objects to be reasonable to the positional changes 
        private void SetPositionsAndRotations() {
            for(int i = 0; i < jointPositions.Count - 1; i++) {
                Vector3 current = jointPositions[i];
                Vector3 next = jointPositions[i + 1];
                Vector3 oldDir = jointStartDirections[i];
                Vector3 newDir = (next - current).normalized;

                Quaternion newRot = Quaternion.FromToRotation(oldDir, newDir);
    
                jointTransforms[i].rotation = newRot * jointStartRotations[i];
            }
            // THIS IS AN ASSUMPTION. TODO find a more graceful way
            endEffector.rotation = jointTransforms[jointTransforms.Count - 1].rotation;

            for(int i = 0; i < jointPositions.Count; i++) {
                jointTransforms[i].position = jointPositions[i];
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

            isValid = !(MISSING_END_EFFECTOR || NON_DESCENDANT_EE);
            if (!isValid && printErr)
            {
                Debug.LogWarning("Invalid Kinematic Chain. " + 
                    (MISSING_END_EFFECTOR ? "- Missing End Effector" : "") +
                    (NON_DESCENDANT_EE ? "- End Effector is not a descendant of Start Bone" : "")
                );
            } 
            return isValid;
        }

        /** 
         * Draws a magenta line between the joints of this kinematic chain
         * MUST BE CALLED IN OnDrawGizmos or OnDrawGizmosSelected!
         */
        private void DrawChainGizmo() {
            if (drawChain) {
                if (isValid) {
                    Transform last = endEffector.transform;
                    Transform current = last;
                    while (current != rootJoint.transform)
                    {
                        current = last.parent.transform;
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawLine(last.transform.position, current.transform.position);
                        last = current;
                    }
                } else {
                    Debug.LogError("Cannot draw gizmos on this chain because it is not valid");
                    drawChain = false;
                }
            }
        }
    }
}
