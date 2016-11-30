using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace NPC {

    public class NPCObject : MonoBehaviour, IPerceivable {

        #region Properties
        [SerializeField]
        public Transform MainInteractionPoint;

        [SerializeField]
        public PERCEIVE_WEIGHT PerceptionWeightType;

        [SerializeField]
        public string Name;

        [SerializeField]
        Transform AnimatedObject;

        Animation g_Animation;

        private bool Open = false;
        private bool OpenState = false;
        private HashSet<Collider> g_Colliders;

        #endregion

        #region Unity_Methods
        void Reset() {
            AnimatedObject = transform.parent;
            MainInteractionPoint = transform;
            PerceptionWeightType = PERCEIVE_WEIGHT.TOTAL;
        }

        private void Start() {
            if(AnimatedObject != null)
                    g_Animation = AnimatedObject.gameObject.GetComponent<Animation>();
        }

        private void OnTriggerEnter(Collider other) {
            Open = true;
            g_Colliders.Add(other);
        }

        private void OnTriggerExit(Collider other) {
            g_Colliders.Remove(other);
            if(g_Colliders.Count == 0)
                Open = false;
        }

        private void FixedUpdate() {
            while(!g_Animation.isPlaying) {
                if(Open) {
                    if(!OpenState) {
                        g_Animation.Play("Sliding_Door");
                    }
                } else {
                    if(OpenState) {
                        g_Animation.Play("Sliding_Door_Close");
                    }
                }
            }
        }

        #endregion


        #region IPerceivable
        public Vector3 GetMainLookAtPoint() {
            return transform.position;
        }

        public PERCEIVEABLE_TYPE GetNPCEntityType() {

            return PERCEIVEABLE_TYPE.OBJECT;
        }

        public PERCEIVE_WEIGHT GetPerceptionWeightType() {
            return PerceptionWeightType;
        }

        public Transform GetTransform() {
            return transform;
        }

        public Vector3 GetCurrentVelocity() {
            return Vector3.zero;  // assume always static for now.
        }

        public Vector3 GetPosition() {
            return transform.position;
        }

        public Vector3 GetForwardDirection() {
            return transform.forward;
        }

        /// <summary>
        /// Colliders need to be taken into consideration for objects, hence
        /// simple radius will not work.
        /// </summary>
        /// <returns>No Implementation Exception</returns>
        public float GetAgentRadius() {
            throw new NotImplementedException();
        }
        #endregion

        #region Utilities
        public override string ToString() {
            return Name;
        }
        #endregion
    }

}