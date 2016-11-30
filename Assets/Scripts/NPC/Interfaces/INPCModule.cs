using UnityEngine;
using System.Collections;

namespace NPC {
    
    public enum NPC_MODULE_TARGET {
        BODY,
        PERCEPTION,
        AI
    }

    public enum NPC_MODULE_TYPE {
        PATHFINDER,
        BEHAVIOR,
        EXPLORATION
    }

    // Anything which implements this interface might be a module of the NPC
    public interface INPCModule {

        bool IsUpdateable();

        void TickModule();

        bool IsEnabled();

        void SetEnable(bool e);

        void RemoveNPCModule();

        NPC_MODULE_TYPE NPCModuleType();

        NPC_MODULE_TARGET NPCModuleTarget();

        string NPCModuleName();

    }
}
