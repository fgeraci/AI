using UnityEngine;
using System.Collections;
using Pathfinding;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

using SupportUtils;
using NPC;

[System.Serializable]
public class NavAStar : MonoBehaviour, IPathfinder, INPCModule {

    #region Members
    [SerializeField]
    public bool EnableNPCModule = true;

    [SerializeField]
    public bool ClearPathOnArrival = false;

    [SerializeField]
    public float DiagonalPenalty = 0f;

    [SerializeField]
    public bool UseHeuristic = true;

    [SerializeField]
    public bool WeightHeuristic = false;

    [SerializeField]
    public float HeuristicWeight = 1f;

    private NPCController g_NPCController;
    private Vector3 g_TargetLocation;
    private Dictionary<NavNode, bool> g_VisitedList;

    private SortedList<float,NavNode> g_Fringe;
    #endregion

    #region Public_Functions

    // f(n) = g(n) + h(n)*e
    public float ComputeNodeCost(NavNode from, NavNode to, GRID_DIRECTION dir) {
        float totalCost = 0f;
        if (GRID_DIRECTION.CURRENT != dir) {
            float modFactor = 1f;
            switch(dir) {
                case GRID_DIRECTION.NORTH_EAST:
                case GRID_DIRECTION.NORTH_WEST:
                case GRID_DIRECTION.SOUTH_EAST:
                case GRID_DIRECTION.SOUTH_WEST:
                    modFactor += DiagonalPenalty;
                    break;
            }
            totalCost = to.Weight * modFactor;
        }
        return totalCost;
    }

    public float ComputeNodeHeuristic(NavNode n) {
        float totalCost = 0f;
        if (UseHeuristic) {
            // euclidean distance heuristic
            totalCost += Vector3.Distance(n.Position, g_TargetLocation)
                * (WeightHeuristic ? HeuristicWeight : 1.0f);
        }
        return totalCost;
    }

    public List<Vector3> ConstructPath(NavNode goal, Dictionary<NavNode,NavNode> parents) {
        List<Vector3> path = new List<Vector3>(parents.Count + 1);
        path.Insert(0, goal.Position);
        bool done = false;
        NavNode curr = goal;
        while (!done) {
            if (curr == parents[curr]) done = true; 
            curr = parents[curr];
            path.Insert(0, curr.Position);
        }
        return path;
    }

    public List<Vector3> FindPath(Vector3 from, Vector3 to) {
        ClearPath();
        RaycastHit hit;
        List<Vector3> pathList = new List<Vector3>();
        if (Physics.Raycast(new Ray(transform.position + (transform.up * 0.2f), -1 * transform.up), out hit)) {
            
            NavGrid grid = hit.collider.GetComponent<NavGrid>();
            NavNode fromNode = grid.GetOccupiedNode(this);
            NavNode goalNode = null;
            g_TargetLocation = to;
            Dictionary<NavNode, float> gVal = new Dictionary<NavNode, float>();
            Dictionary<NavNode, float> fVal = new Dictionary<NavNode, float>();
            HashSet<NavNode> closedList = new HashSet<NavNode>();
            Dictionary<NavNode, NavNode> parents = new Dictionary<NavNode, NavNode>();
            
            g_Fringe = new SortedList<float, NavNode>(new DuplicateKeyComparer<float>());
            parents.Add(fromNode, fromNode);
            gVal.Add(fromNode, 0f);
            fVal.Add(fromNode,ComputeNodeHeuristic(fromNode));
            g_Fringe.Add(fVal[fromNode], fromNode);
            closedList.Add(fromNode);


            while (g_Fringe.Count > 0) {

                // get next best node
                NavNode n = g_Fringe.Values[0];
                g_Fringe.RemoveAt(0);
                closedList.Remove(fromNode);
                closedList.Add(fromNode);
                
                // test goal
                if (IsCurrentStateGoal(new System.Object[] { n })) {
                    n.SetHighlightTile(true, Color.green, 1f);
                    goalNode = n;
                    pathList = ConstructPath(goalNode, parents);
                    return pathList;
                }

                // loop adjacent
                Dictionary<NavNode, GRID_DIRECTION> neighbors = grid.GetNeighborNodes(n);
                foreach(NavNode neighbor in neighbors.Keys) {
                    if(!closedList.Contains(neighbor) && neighbor.IsWalkable()) {
                        float val = gVal[n] + ComputeNodeCost(n, neighbor, neighbors[neighbor]);
                        bool inFringe = closedList.Contains(neighbor);
                        if (!inFringe || val <= gVal[n]) {
                            gVal.Add(neighbor,val);
                            fVal.Add(neighbor,ComputeNodeHeuristic(neighbor));
                            parents.Add(neighbor, n);
                            if(!inFringe) {
                                g_Fringe.Add(fVal[neighbor], neighbor);
                                closedList.Add(neighbor);
                            }
                        }
                    }
                }
            }
        } else {
            g_NPCController.Debug("NavAStar --> Pathfinder not on grid");    
        }
        return pathList;
    }

    public void ClearPath() {
        foreach(NavNode n in g_VisitedList.Keys) {
            n.SetHighlightTile(false, Color.black, 0f);
        }
    }

    public bool IsEnabled() {
        return EnableNPCModule;
    }

    public bool IsReachable(Vector3 from, Vector3 target) {
        throw new NotImplementedException();
    }

    public string NPCModuleName() {
        return "A* Algorithm";
    }

    public NPC_MODULE_TYPE NPCModuleType() {
        return NPC_MODULE_TYPE.PATHFINDER;
    }

    public NPC_MODULE_TARGET NPCModuleTarget() {
        return NPC_MODULE_TARGET.AI;
    }

    public void SetEnable(bool e) {
        EnableNPCModule = e;
    }

    public string ObjectIdentifier() {
        return gameObject.name;
    }

    public void RemoveNPCModule() {
        GetComponent<NPCController>().RemoveNPCModule(this);
    }
    #endregion

    #region Unity_methods

    void Start() {
        g_NPCController = GetComponent<NPCController>();
        g_Fringe = new SortedList<float, NavNode>(new DuplicateKeyComparer<float>());
        g_VisitedList = new Dictionary<NavNode, bool>();
    }

    void Update() {
        if(g_TargetLocation != Vector3.zero
            && ClearPathOnArrival 
            && Vector3.Distance(g_NPCController.transform.position, g_TargetLocation) < 0.5f) {
            ClearPath();
            g_TargetLocation = Vector3.zero;
        }
    }

    void OnDestroy() {
        RemoveNPCModule();
    }

    public bool IsCurrentStateGoal(System.Object[] states = null) {
        NavNode curNode = (NavNode)states[0];
        return Vector3.Distance(curNode.Position, g_TargetLocation) < curNode.Radius * 2f;
    }

    #endregion

}
