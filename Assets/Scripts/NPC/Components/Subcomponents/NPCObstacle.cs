using UnityEngine;
using System.Collections;
using System;

namespace NPC {

    public class NPCObstacle : MonoBehaviour, IPerceivable {

        public Vector3 CalculateAgentRepulsionForce(IPerceivable p) {
            throw new NotImplementedException();
        }

        public Vector3 CalculateAgentSlidingForce(IPerceivable p) {
            throw new NotImplementedException();
        }

        public Vector3 CalculateRepulsionForce(IPerceivable p) {
            throw new NotImplementedException();
        }

        public Vector3 CalculateSlidingForce(IPerceivable p) {
            throw new NotImplementedException();
        }

        public float GetAgentRadius() {
            return 0f;
        }

        public Vector3 GetCurrentVelocity() {
            return new Vector3(0f,0f,0f);
        }

        public Vector3 GetForwardDirection() {
            return transform.forward;
        }

        public Vector3 GetMainLookAtPoint() {
            return transform.position;
        }

        public PERCEIVEABLE_TYPE GetNPCEntityType() {
            return PERCEIVEABLE_TYPE.OBJECT;
        }

        public PERCEIVE_WEIGHT GetPerceptionWeightType() {
            Rigidbody rb = GetComponent<Rigidbody>();
            return PERCEIVE_WEIGHT.TOTAL;
        }

        public Vector3 GetPosition() {
            return transform.position;
        }

        public Transform GetTransform() {
            return transform;
        }
    }

}