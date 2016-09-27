using UnityEngine;
using System.Collections;

namespace NPC {

    public enum PERCEIVE_WEIGHT {
        NONE,
        WEIGHTED,
        TOTAL
    }

    public interface IPerceivable {
        PERCEIVE_WEIGHT GetPerceptionWeightType();
    }

}
