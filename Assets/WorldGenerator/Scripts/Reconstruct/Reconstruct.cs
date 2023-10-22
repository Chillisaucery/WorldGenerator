using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class Reconstruct : MonoBehaviour
{
    [SerializeField]
    Transform objectCollection;

    [SerializeField]
    int meshIndex = 0;



    List<MeshFilter> meshFilters = new List<MeshFilter>();
    Dictionary<Edge, int> _edgesTrisMap = new Dictionary<Edge, int>();
    List<(Vector3 coord, int index)> _points = new List<(Vector3 coord, int index)>();

    List<Edge> _boundaryEdges = new List<Edge>();
    Mesh _mesh = null;
    public List<(Vector3 coord, int index)> Points { get => _points; set => _points = value; }
    public List<Edge> BoundaryEdges { get => _boundaryEdges; set => _boundaryEdges = value; }
    public Mesh Mesh { get => _mesh; set => _mesh = value; }

    private void OnEnable()
    {
        meshFilters = objectCollection.GetComponentsInChildren<MeshFilter>().ToList();
        BeginReconstruct();
    }



    public void BeginReconstruct()
    {
        Mesh mesh = meshFilters[meshIndex].sharedMesh;
        _mesh = mesh;

        _edgesTrisMap.Clear();

        //Loop through the faces
        for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
        {
            int[] triangles = mesh.GetTriangles(subMeshIndex);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int vertexIndex1 = triangles[i];
                int vertexIndex2 = triangles[i + 1];
                int vertexIndex3 = triangles[i + 2];

                Edge edge12 = new Edge(vertexIndex1, vertexIndex2);
                Edge edge23 = new Edge(vertexIndex2, vertexIndex3);
                Edge edge31 = new Edge(vertexIndex3, vertexIndex1);

                AddConnectivity(_edgesTrisMap, edge12);
                AddConnectivity(_edgesTrisMap, edge23);
                AddConnectivity(_edgesTrisMap, edge31);

                /*Debug.Log("Edge: " + vertexIndex1 + "-" + vertexIndex2);
                Debug.Log("Edge: " + vertexIndex2 + "-" + vertexIndex3);
                Debug.Log("Edge: " + vertexIndex3 + "-" + vertexIndex1);*/
            }
        }

        /*_edgesTrisMap = _edgesTrisMap.OrderBy(e => e.Key.vertex1).ToDictionary(e => e.Key, e => e.Value);

        foreach (var e in _edgesTrisMap)
        {
            Debug.Log("Key: " + (e.Key, e.Value));
        }*/

        List<Edge> boundaryEdges = _edgesTrisMap.Where(e => e.Value <= 1).ToDictionary(e => e.Key, e => e.Value).Keys.ToList();
        _boundaryEdges = boundaryEdges;
        //Remove all the vertex duplications

        Debug.Log("Edge count: " + boundaryEdges.Count);    //Should be 65 for the cube

        //boundaryEdges.ForEach(e => Debug.Log("Vertex: " + (e.vertex1, e.vertex2)));

        ExtractPoints(mesh, boundaryEdges);
    }

    private void ExtractPoints(Mesh mesh, List<Edge> boundaryEdges)
    {
        List<int> vertexIndices = new List<int>();

        foreach (Edge edge in boundaryEdges)
        {
            vertexIndices.Add(edge.vertex1);
            vertexIndices.Add(edge.vertex2);
        }

        vertexIndices.Distinct();

        _points.Clear();
        foreach (int vertexIndex in vertexIndices)
        {
            _points.Add((mesh.vertices[vertexIndex] + meshFilters[meshIndex].transform.position, vertexIndex));
        }

        _points = _points.OrderBy(point => point.coord.x)
                        .ThenBy(point => point.coord.y)
                        .ThenBy(point => point.coord.z)
                        .ToList();

        //Remove duplications
        List<(Vector3 coord, int index)> duplicatedPoints = new List<(Vector3 coord, int index)>();

        for (int i = 0; i < _points.Count - 1; i++)
        {
            if ((_points[i + 1].coord == _points[i].coord))
            {
                duplicatedPoints.Add(_points[i+1]);
            }
        }

        //Find the duplicated coord
        List<Vector3> duplicateCoordinates = duplicatedPoints.GroupBy(x => x.coord)
                                .Where(group => group.Count() > 1)
                                .Select(group => group.Key)
                                .ToList();

        // Remove the duplicateCoordinates from the original list
        foreach (Vector3 coord in duplicateCoordinates)
        {
            duplicatedPoints.RemoveAll(x => x.coord == coord);
        }

        //duplicatedPoints.ForEach(e => Debug.Log("Hahah: " + e.coord));

        _points = duplicatedPoints;
    }



    private void OnDrawGizmos()
    {
        /*Mesh mesh = meshFilters[meshIndex].sharedMesh;

        Gizmos.color = Color.black;
*/
        /*foreach (var edge in _edgesTrisMap)
        {
            Gizmos.DrawLine(mesh.vertices[edge.Key.vertex1], mesh.vertices[edge.Key.vertex2]);
        }*/

        Gizmos.color = Color.red;
        for (int i = 0; i < _points.Count; i++)
        {
            Vector3 point = _points[i].coord;
            int index = _points[i].index;
            Gizmos.DrawSphere(point, 0.01f);
            Handles.Label(point, index.ToString());
        }
    }

    private static void AddConnectivity(Dictionary<Edge, int> edgesTrisMap, Edge edge)
    {
        if (edgesTrisMap.ContainsKey(edge))
            edgesTrisMap[edge] += 1;
        else edgesTrisMap.Add(edge, 1);
    }



    public class Edge
    {
        public int vertex1 = 0, vertex2 = 0;

        public Edge(int vertex1, int vertex2)
        {
            this.vertex1 = vertex1;
            this.vertex2 = vertex2;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Edge otherEdge = (Edge)obj;
            return (vertex1 == otherEdge.vertex1 && vertex2 == otherEdge.vertex2) ||
                   (vertex1 == otherEdge.vertex2 && vertex2 == otherEdge.vertex1);
        }

        public override int GetHashCode()
        {
            return vertex1.GetHashCode() ^ vertex2.GetHashCode();
        }

        public override string ToString() => vertex1 + ":" + vertex2; 
    }
}
