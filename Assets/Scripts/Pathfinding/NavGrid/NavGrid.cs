﻿using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;


namespace Pathfinding {

    public enum GRID_SCALE {
        ONE = 1,
        HALF = 2,
        QUARTER = 4
    }

    public enum GRID_DIRECTION {
        CURRENT,
        NORTH,
        SOUTH,
        EAST,
        WEST,
        NORTH_EAST,
        NORTH_WEST,
        SOUTH_EAST,
        SOUTH_WEST
    }

    public class NavGrid : MonoBehaviour {

        #region Properties
        #endregion

        #region Members
        private Dictionary<IPathfinder, NavNode> g_WalkedOnNodes;
        public bool         WriteGridToFile = false;
        public string       FileName = "Grid_Description.txt";
        public bool         RedrawGrid;
        public bool         CreateRivers;
        public LayerMask    UnwalkableMask;
        public Vector2      GridDimensions;
        public GRID_SCALE   GridScale = GRID_SCALE.ONE;
        private float       g_GridScale = 1.0f;
        public bool         PaintGridOnScene = false;
        public bool         PaintPathdOnPlay = false;
        public bool         DisplayTileText = false;
        public float        GridTransparency = 1.0f;
        private float       g_NodeRadius = 0.5f;
        public int          RandomHeavyAreas = 8;       // Default value
        public int          RandomHighways = 4;         // Default value
        public float        BlockingHeight = 2.0f;
        NavNode[,]          g_Grid;
        public float        EasyWeight = (float)        NavNode.NODE_TYPE.HIGHWAY;
        public float        NormalWeight = (float)      NavNode.NODE_TYPE.WALKABLE;
        public float        MediumWeight = (float)      NavNode.NODE_TYPE.HARD_TO_WALK;
        public float        NotAvailableWeight = (float)NavNode.NODE_TYPE.NONWALKABLE;
        private bool        g_TileSelected = false;
        private NavNode     g_SelectedTile;
        public float        SelectedTileWeight = 1;
        public Vector2      SelectedTile;
        #endregion

        #region Private_Functions

        private void PopulateGrid() {
            g_WalkedOnNodes = new Dictionary<IPathfinder, NavNode>();
            g_GridScale = 1f / (float) GridScale;
            float nodeDiameter = g_NodeRadius * 2 * g_GridScale;
            GridDimensions.x = Mathf.RoundToInt(transform.localScale.x / nodeDiameter);
            GridDimensions.y = Mathf.RoundToInt(transform.localScale.z / nodeDiameter);
            int tilesX = (int) GridDimensions.x,
                tilesY = (int) GridDimensions.y;
            g_Grid = new NavNode[tilesX, tilesY];
            Vector3 gridWorldBottom = (transform.position - (transform.right * GridDimensions.x / 2) -
                (transform.forward * GridDimensions.y / 2) + new Vector3(g_NodeRadius,0.0f,g_NodeRadius)) * g_GridScale;
            for(int row = 0; row < tilesX; ++row) {
                for (int col = 0; col < tilesY; ++col) {
                    NavNode node = new NavNode(
                        gridWorldBottom + (transform.right * (nodeDiameter) * row) + transform.forward * (nodeDiameter) * col,
                        new Vector2(row,col),
                        transform.up,
                        BlockingHeight,
                        true,
                        g_NodeRadius * g_GridScale,
                        this);
                    g_Grid[row, col] = node;
                }
            }
            RandomizeHardWalkingAreas(RandomHeavyAreas, 31);
            RandomizeHighways(RandomHighways);
        }

        private void RandomizeHardWalkingAreas(int areas, int spread) {

            Dictionary<int, int> a = new Dictionary<int, int>();

            int limitX = g_Grid.GetLength(0) - 1, 
                limitY = g_Grid.GetLength(1) - 1;

            Vector2[] randomAreas = new Vector2[areas];

            for(int i = 0; i < areas; ++i) {

                Vector2 pos = new Vector2(
                    Mathf.RoundToInt(Random.Range(0, limitX)),
                    Mathf.RoundToInt(Random.Range(0, limitY)));

                int xOff = (int)(pos.x - (spread / 2)),
                    yOff = (int)(pos.y - (spread / 2));

                for (int r = 0; r < spread; ++r) {
                    for (int c = 0; c < spread; ++c) {
                        if (IsValid(new Vector2(xOff + r, yOff + c))) {
                            g_Grid[xOff + r, yOff + c].Weight = (float) NavNode.NODE_TYPE.HARD_TO_WALK;

                        }
                    }
                }
            }
        }

        private void RandomizeHighways(int randomHighways) {

            int totalRes = 0;

        restart_highways:

            Dictionary<NavNode,bool> totalChange = new Dictionary<NavNode, bool>();
            bool done = false;
            int restarts = -1;
            
            while (!done) {

                // for each path
                for (int i = 0; i < RandomHighways; ++i) {

                start_highway:
                    restarts++;
                    if(restarts > 400) {
                        Debug.Log("Restarting all highways");
                        totalRes++;
                        if (totalRes > 20) return;
                        goto restart_highways;
                    }
                    // get base
                    int xBase = 0,
                        yBase = 0;

                    bool onX = false;
                    if(Random.Range(0f,1f) <= 0.5f) {
                        xBase = (int) Random.Range(0, g_Grid.GetLength(0) - 1);
                        yBase = 0;
                        onX = true;
                    } else {
                        xBase = 0;
                        yBase = (int) Random.Range(0 , g_Grid.GetLength(1) - 1);
                    }

                    Dictionary<NavNode,bool> toChange = new Dictionary<NavNode, bool>();
                    int count = 0;

                    // is it a valid start?
                    if (IsValid(new Vector2(xBase, yBase))
                        // first node is not a highway
                        && !g_Grid[xBase, yBase].IsType(NavNode.NODE_TYPE.HIGHWAY)) {

                        NavNode n = g_Grid[xBase, yBase];

                        // 20 forth by default
                        for (int f = 0; f < 20; ++f) {
                            if (IsValid(new Vector2(xBase, yBase))
                                && !g_Grid[xBase, yBase].IsType(NavNode.NODE_TYPE.HIGHWAY)) {
                                if(!onX)
                                    toChange.Add(g_Grid[xBase++, yBase], true);
                                else
                                    toChange.Add(g_Grid[xBase, yBase++], true);
                                ++count;
                            } else goto start_highway;
                        }

                        bool randomWalk = true;

                        bool xWalk = !onX, yWalk = onX;

                        while (randomWalk) {
                            
                            if (xWalk)
                                yBase += 1;
                            else
                                xBase += yWalk ? 1 : -1;

                            bool error = false;

                            if (IsValid(new Vector2(xBase, yBase))) {
                                if (!totalChange.ContainsKey(g_Grid[xBase, yBase])) {
                                    toChange.Add(g_Grid[xBase, yBase], true);
                                    ++count;
                                } else {
                                    Debug.Log("Collision on random walk");
                                    error = true;
                                }
                            } else {
                                if (count < 100) {
                                    error = true;
                                } else {
                                    randomWalk = false;
                                }
                            }

                            if(error) {
                                goto start_highway;
                            }

                            if (count % 20 == 0) {
                                float turn = Random.Range(0f, 1f);
                                if (turn >= 0.6f) {
                                    if (xBase == 0)
                                        xWalk = true;
                                    else if (yBase == 0)
                                        yWalk = true;
                                    else {
                                        xWalk = !xWalk;
                                        yWalk = !yWalk;
                                    }
                                }
                            }
                        }

                        foreach (NavNode node in toChange.Keys) {
                            if (totalChange.ContainsKey(node)) {
                                Debug.Log("Path collides, re-start path");
                                goto start_highway;
                            } else totalChange.Add(node, true);
                        }

                    } else --i; // repeat
                }
                done = true;
            }
            foreach(NavNode n in totalChange.Keys) {
                n.Weight -= n.Weight + (float) NavNode.NODE_TYPE.HIGHWAY;
            }
        }

        #endregion Private_Functions

        #region Unity_Methods

        // Use this for initialization
        void Reset() {
            PopulateGrid();
        }
        
        void Awake() {
            PopulateGrid();
            g_WalkedOnNodes = new Dictionary<IPathfinder, NavNode>();
            if(WriteGridToFile) {
                if(File.Exists(FileName)) {
                    FileName = FileName.Substring(0, FileName.IndexOf(".txt")) + "_copy.txt";
                    Debug.Log("File " + FileName + " already exists - creating copy: " + FileName);
                    // TODO - create file here
                }
                StreamWriter sw = File.CreateText(FileName);
                sw.WriteLine("Grid Specs");
                sw.WriteLine("-----------\n\n");
                sw.Close();
            }
        }

        void Update() {
            SelectedTile.x = Mathf.Clamp(SelectedTile.x, 0, g_Grid.GetLength(0) - 1);
            SelectedTile.y = Mathf.Clamp(SelectedTile.y, 0, g_Grid.GetLength(1) - 1);
            foreach (NavNode node in g_Grid) {
                node.IsWalkable();
                node.SetActiveTileText(DisplayTileText);
                
            }
        }

        void OnDestroy() {
            foreach(Transform t in transform) {
                GameObject.DestroyImmediate(t.gameObject);
            }
        }

        // Update is called once per frame
        
        void OnDrawGizmos() {

            if (NotAvailableWeight < MediumWeight) {
                NotAvailableWeight = MediumWeight + 1;
            } else if (MediumWeight < NormalWeight) {
                MediumWeight = NormalWeight + 1;
            } else if (NormalWeight <= 0)
                NormalWeight = 1f;

            // x for rows, y for cols
            Gizmos.DrawWireCube(transform.position, new Vector3(GridDimensions.x * g_GridScale, transform.position.y , GridDimensions.y * g_GridScale));
            if(g_Grid == null || g_Grid.GetLength(0) != GridDimensions.x || g_Grid.GetLength(1) != GridDimensions.y || RedrawGrid) {
                Reset();
                RedrawGrid = false;
            } else if (PaintGridOnScene) {

                SelectedTile.x = Mathf.Clamp(SelectedTile.x, 0, g_Grid.GetLength(0) - 1);
                SelectedTile.y = Mathf.Clamp(SelectedTile.y, 0, g_Grid.GetLength(1) - 1);

                if (g_SelectedTile != null) {
                    g_SelectedTile.Weight = SelectedTileWeight > 0 ? SelectedTileWeight : 1;
                    g_SelectedTile.Selected = false;
                }
                NavNode tmp = g_Grid[(int)SelectedTile.x, (int)SelectedTile.y];
                if (g_SelectedTile != tmp) {
                    SelectedTileWeight = tmp.Weight;
                }
                g_SelectedTile = tmp;
                g_SelectedTile.Selected = true;
                g_SelectedTile = tmp;

                foreach(NavNode node in g_Grid) {
                    if(!Application.isPlaying)
                        node.IsWalkable();
                    Color c;
                    if(node.Selected) {
                        c = Color.white;
                        c.a = 1.0f * GridTransparency;
                    } else {
                        if (node.Weight >= NotAvailableWeight || !node.Available) {
                            c = Color.red;
                            c.a = 0.5f * GridTransparency;
                        } else if (node.Weight >= MediumWeight) {
                            c = Color.yellow;
                            c.a = 0.3f * GridTransparency;
                        } else if (node.Weight >= NormalWeight) {
                            c = Color.green;
                            c.a = 0.1f * GridTransparency;
                        } else {
                            c = Color.blue;
                            c.a = 0.8f * GridTransparency;
                        }
                    }

                    Gizmos.color = c;
                    float diam = node.Radius * 2;
                    Gizmos.DrawWireCube(node.Position, new Vector3(diam, transform.position.y, diam));
                    Gizmos.color = Color.white;
                }
            }
        }
        #endregion

        #region Public_Functions
        
        public void SetIPathfinderNode(IPathfinder ipf, NavNode node) {
            if(g_WalkedOnNodes.ContainsKey(ipf) && g_WalkedOnNodes[ipf] != node) {
                if(Application.isPlaying) g_WalkedOnNodes[ipf].SetHighlightTile(false, Color.grey, 0.5f);
                g_WalkedOnNodes.Remove(ipf);
            } else if(!g_WalkedOnNodes.ContainsKey(ipf)) {
                g_WalkedOnNodes.Add(ipf, node);
                if (Application.isPlaying) node.SetHighlightTile(true, Color.red, 0.5f);
            }
        }

        public NavNode GetOccupiedNode(IPathfinder ipf) {
            return g_WalkedOnNodes[ipf];
        }

        public bool IsValid(Vector2 coord) {
            return 
                !(coord.x < 0 || coord.y < 0) &&
                (coord.x < g_Grid.GetLength(0)) && 
                (coord.y < g_Grid.GetLength(1));
        }
        public NavNode GetNeighborNode(NavNode current, GRID_DIRECTION dir) {
            return null;
        }

        /// <summary>
        /// Returns all exisitng neighbors
        /// </summary>
        /// <param name="current node"></param>
        /// <returns></returns>
        public Dictionary<NavNode,GRID_DIRECTION> GetNeighborNodes(NavNode current) {

            Dictionary<NavNode, GRID_DIRECTION> neighbors = new Dictionary<NavNode, GRID_DIRECTION>();
            int x = (int) current.GridPosition.x, y = (int) current.GridPosition.y;
            
            for(int i = -1; i < 2; ++i) {

                if (x + i >= g_Grid.GetLength(0)) continue;             // skip east
                else if (x + i < 0) continue;                           // skip west

                for (int j = 1; j > -2; --j) {

                    if (i == 0 && j == 0) continue;                     // skip center
                    else if (y + j >= g_Grid.GetLength(1)) continue;    // skip north
                    else if (y + j < 0) continue;                       // skip south 

                    GRID_DIRECTION dir = GRID_DIRECTION.CURRENT;        // dummy
                    if      (i == -1 && j ==  1) dir = GRID_DIRECTION.NORTH_WEST;
                    else if (i ==  0 && j ==  1) dir = GRID_DIRECTION.NORTH;
                    else if (i ==  1 && j ==  1) dir = GRID_DIRECTION.NORTH_EAST;
                    else if (i == -1 && j ==  0) dir = GRID_DIRECTION.WEST;
                    else if (i ==  1 && j ==  0) dir = GRID_DIRECTION.EAST;
                    else if (i ==  0 && j == -1) dir = GRID_DIRECTION.SOUTH;
                    else if (i == -1 && j == -1) dir = GRID_DIRECTION.SOUTH_WEST;
                    else if (i ==  1 && j == -1) dir = GRID_DIRECTION.SOUTH_EAST;
                    neighbors.Add(g_Grid[x + i, y + j], dir);

                }
            }

            return neighbors;
        }
        #endregion
    }
}
