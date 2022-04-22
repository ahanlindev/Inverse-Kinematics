using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ahanlindev
{
    public class IKJoint : MonoBehaviour
    {
        [SerializeField] private HashSet<IKJoint> childBones;
        // Start is called before the first frame update
        void Start()
        {
            childBones = new HashSet<IKJoint>();
            updateChildBones();
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        private void updateChildBones()
        {
            foreach (Transform child in transform)
            {
                IKJoint childBone = child.GetComponent<IKJoint>();
                if (childBone != null && !child.Equals(transform))
                {
                    childBones.Add(childBone);
                }
            }
        }
    }
}
