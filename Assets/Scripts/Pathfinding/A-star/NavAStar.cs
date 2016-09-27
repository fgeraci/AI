using UnityEngine;
using System.Collections;
using Pathfinding;
using System;
using System.Collections.Generic;

using SupportUtils;
using NPC;

[System.Serializable]
public class NavAStar : MonoBehaviour, IPathfinder, INPCModule {

    #region Members
    [SerializeField]
    public bool EnableNPCModule = true;

    [SerializeField]
    public bool DelayPathFinder = false;

    [SerializeField]
    public int DelayTime = 1;

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
    public float ComputeNodeCost(NavNode n, GRID_DIRECTION dir) {
        float modFactor = 1f;
        switch(dir) {
            case GRID_DIRECTION.NORTH_EAST:
            case GRID_DIRECTION.NORTH_WEST:
            case GRID_DIRECTION.SOUTH_EAST:
            case GRID_DIRECTION.SOUTH_WEST:
                modFactor += DiagonalPenalty;
                break;
        }
        float totalCost = n.Weight * modFactor;
        if(UseHeuristic) {
            totalCost += Vector3.Distance(n.Position, g_TargetLocation) 
                * (WeightHeuristic ? HeuristicWeight : 1.0f);
        }
        return totalCost;
    }

    public List<Vector3> FindPath(Vector3 from, Vector3 to) {
        ClearPath();
        List<Vector3> pathList = new List<Vector3>();
        RaycastHit hit;
        bool found = false;

        if (Physics.Raycast(new Ray(transform.position + (transform.up * 0.2f), -1 * transform.up), out hit)) {

            NavGrid grid = hit.collider.GetComponent<NavGrid>();
            NavNode currentNode = grid.GetOccupiedNode(this);
            g_NPCController.Debug("NavAStar --> A* working for " + this + ", starting from: " + currentNode);
            g_TargetLocation = to;
            g_VisitedList = new Dictionary<NavNode, bool>();
            /* Algo implementation */
            g_Fringe.Clear();
            g_Fringe.Add(ComputeNodeCost(currentNode, GRID_DIRECTION.CURRENT),currentNode);

            while(g_Fringe.Count > 0) {
                NavNode n = g_Fringe.Values[0];
                g_Fringe.RemoveAt(0);
                // A* is optimal, so the current node is always optimal
                pathList.Add(n.Position);
                n.SetHighlightTile(true, Color.yellow, 0.7f);
                Dictionary<NavNode, GRID_DIRECTION> neighbors = grid.GetNeighborNodes(n);
                if(DelayPathFinder) {
                    System.Threading.Thread.Sleep(DelayTime * 1000);
                }
                foreach(NavNode adj in neighbors.Keys) {
                    if(!g_VisitedList.ContainsKey(adj)) {
                        g_VisitedList.Add(adj, true);
                        if(adj.Available) {
                            if(IsCurrentStateGoal(new System.Object[] { adj })) {
                                pathList.Add(adj.Position);
                                adj.SetHighlightTile(true, Color.green, 1f);
                                found = true;
                                goto finder_exit;
                            } else {
                                float val = ComputeNodeCost(adj, neighbors[adj]);
                                adj.Weight = (int) val;
                                g_Fringe.Add(val, adj);
                                adj.SetHighlightTile(true, Color.white, 0.3f);
                            }
                        }
                    }
                }
            }
        } else {
            g_NPCController.Debug("NavAStar --> Pathfinder not on grid");    
        }
    finder_exit:
        if (!found) pathList.Clear();
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
        return Vector3.Distance(curNode.Position, g_TargetLocation) < curNode.Radius * 1.5f;
    }

    #endregion

}
