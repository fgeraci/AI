using System;
using UnityEngine;
using System.Collections;

namespace NPC {

    [AttributeUsage(AttributeTargets.Method)]
    public class NPCAffordance : System.Attribute {

        public string Name;
	
        public NPCAffordance(string name) {
            this.Name = name;
        }

    }

}
