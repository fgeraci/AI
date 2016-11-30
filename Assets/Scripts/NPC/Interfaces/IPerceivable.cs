using UnityEngine;
using System.Collections;

namespace NPC {

    public enum PERCEIVEABLE_TYPE {
        NPC,
        OBJECT
    }

    public enum PERCEIVE_WEIGHT {
        NONE,
        WEIGHTED,
        TOTAL
    }

    public interface IPerceivable {
        PERCEIVEABLE_TYPE GetNPCEntityType();
        PERCEIVE_WEIGHT GetPerceptionWeightType();
        Transform   GetTransform();
        Vector3     GetCurrentVelocity();
        Vector3     GetPosition();
        Vector3     GetForwardDirection();
        float       GetAgentRadius();
        Vector3     GetMainLookAtPoint();
    }

}
