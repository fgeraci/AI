using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding {
    
    public interface IPathfinder {

        bool IsCurrentStateGoal(System.Object[] states = null);

        string ObjectIdentifier();

        List<Vector3> FindPath(Vector3 from, Vector3 to);

        bool IsReachable(Vector3 from, Vector3 to);

        float ComputeNodeCost(NavNode n, GRID_DIRECTION dir);

        void ClearPath();
	
    }
}
