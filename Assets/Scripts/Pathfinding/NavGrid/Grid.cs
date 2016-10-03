using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Grid : MonoBehaviour {

    #region Members
    private Vector3[] g_GridVertices;
    private MeshFilter g_MeshFilter;
    #endregion

    #region Properties
    public int Rows = 10;
    public int Columns = 10;
    public float TileRadius = 0.5f;
    public bool DrawGridOnScene = true;
    #endregion

    #region Unity_Functions
    void Reset() {
        InitializeGrid();
    }

    void OnDrawGizmos() {
        if(DrawGridOnScene) {
            if(g_GridVertices != null) {
                foreach (Vector3 v in g_GridVertices) {
                    Gizmos.DrawSphere(transform.TransformPoint(v), 0.1f);
                }
            }
        }
    }
    #endregion

    #region Private_Functions
    private void InitializeGrid() {
        g_GridVertices = new Vector3[(Rows + 1) * (Columns + 1)];
        float tileDiameter = TileRadius * 2f;
        Vector3 bottomVertex = transform.position - transform.right * (Columns / 2) * tileDiameter
            - transform.forward * (Columns / 2) * tileDiameter;
        int i = 0;
        for (int r = 0; r < (Rows + 1); ++r) {
            for (int c = 0; c < (Columns + 1); ++c) {
                g_GridVertices[i++] = bottomVertex + new Vector3(tileDiameter * c, 0f, tileDiameter * r);
            }
        }
    }
    #endregion
}