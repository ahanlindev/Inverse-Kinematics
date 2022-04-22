using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ahanlindev
{
    [Serializable]
    public class KinematicChain
    {
        [Tooltip("A string representing the name of this kinematic chain")]
        public string name = "Kinematic Chain";
        [Tooltip("The highest bone on the kinematic chain")]
        public IKJoint start;
        [Tooltip("The end effector of the kinematic chain. MUST BE A DESCENDANT OF THE START")]
        public IKJoint endEffector;
        [Tooltip("Object or point in space targeted by the end effector")]
        public Transform target;
        [Tooltip("If gizmos are enabled, represent the chain as lines between the bones")]
        public bool showChain;
        [HideInInspector] public bool isValid = true; // marked true if anything is broken
    }

    public class IKSkeleton : MonoBehaviour
    {
        [SerializeField] private List<KinematicChain> chains;
        // Start is called before the first frame update
        void Start()
        {
            checkChainsAreValid();
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        private void OnDrawGizmos()
        {
            foreach (KinematicChain chain in chains)
            {
                
                if (chain.showChain && chain.isValid)
                {
                    Transform last = chain.endEffector.transform;
                    Transform current = last;
                    int debugctr = 0;
                    while (current != chain.start.transform && debugctr++ < 500)
                    {
                        current = last.parent.transform;
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawLine(last.transform.position, current.transform.position);
                        last = current;
                        if (current == null) Debug.LogError("Null bone in kinematic chain \"" + chain.name + "\"");
                    }
                }
            }
        }

        // Prints a debug log if any kinematic chains have invalid fields
        private void checkChainsAreValid()
        {

            foreach (KinematicChain chain in chains)
            {
                bool MISSING_NAME = chain.name.Equals("");
                bool MISSING_END_EFFECTOR = chain.endEffector == null;
                bool MISSING_START_BONE = chain.start == null;
                bool NON_DESCENDANT_EE = !(MISSING_END_EFFECTOR || MISSING_START_BONE);

                if (!MISSING_START_BONE)
                {
                    IKJoint[] children = chain.start.GetComponentsInChildren<IKJoint>();
                    foreach (IKJoint child in children)
                    {
                        if (child.Equals(chain.endEffector))
                        {
                            NON_DESCENDANT_EE = false;
                            break;
                        }
                    }
                }
                chain.isValid = !(MISSING_NAME || MISSING_END_EFFECTOR || MISSING_START_BONE || NON_DESCENDANT_EE);
                if (!chain.isValid)
                {

                    Debug.LogError("Invalid Kinematic Chain \"" + chain.name + "\". \nExpand for details." + 
                        (MISSING_NAME ? "\n  - Missing Name" : " ") + 
                        (MISSING_START_BONE ? "\n  - Missing Start Bone" : " ") + 
                        (MISSING_END_EFFECTOR ? "\n  - Missing End Effector" : " ") +
                        (NON_DESCENDANT_EE ? "\n  - End Effector is not a descendant of Start Bone" : " ")
                    );
                } 
            }
        }
    }
}
