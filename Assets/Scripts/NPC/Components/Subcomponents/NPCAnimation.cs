using System;
using UnityEngine;
using System.Collections;

namespace NPC {

    public enum ANIMATION_PARAM_TYPE {
        BOOLEAN,
        FLOAT,
        TRIGGER
    }
    
    public enum ANIMATION_LAYER {
        FULL_BODY,
        GESTURE
    }

    [AttributeUsage(AttributeTargets.All)]
    public class NPCAnimation : System.Attribute {
        public string Name;
        public int AnimationHash;
        public ANIMATION_PARAM_TYPE ParamType;
        public ANIMATION_LAYER Layer;
        public NPCAnimation(string name, ANIMATION_PARAM_TYPE paramType, ANIMATION_LAYER layer) {
            this.Name = name;
            this.ParamType = paramType;
            this.Layer = layer;
        }
    }
}