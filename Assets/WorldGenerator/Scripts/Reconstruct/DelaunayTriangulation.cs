using UnityEngine;
using System.Collections.Generic;

namespace Triangulation
{
    public class DelaunayTriangulation
    {
        public List<Triangle> Triangles { get; private set; }

        public void CreateTriangulation(List<Vector3> points)
        {
            Triangles = new List<Triangle>();

            // Add a super triangle that encompasses all input points
            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
            foreach (Vector3 point in points)
            {
                if (point.x < minX) minX = point.x;
                if (point.y < minY) minY = point.y;
                if (point.z < minZ) minZ = point.z;
                if (point.x > maxX) maxX = point.x;
                if (point.y > maxY) maxY = point.y;
                if (point.z > maxZ) maxZ = point.z;
            }
            float dx = maxX - minX;
            float dy = maxY - minY;
            float dz = maxZ - minZ;
            float deltaMax = Mathf.Max(dx, Mathf.Max(dy, dz));
            float midx = (minX + maxX) / 2f;
            float midy = (minY + maxY) / 2f;
            float midz = (minZ + maxZ) / 2f;

            Vector3 p1 = new Vector3(midx - 20 * deltaMax, midy - deltaMax, midz);
            Vector3 p2 = new Vector3(midx, midy + 20 * deltaMax, midz);
            Vector3 p3 = new Vector3(midx + 20 * deltaMax, midy - deltaMax, midz);
            Triangle superTriangle = new Triangle(p1, p2, p3);

            // Add the super triangle to the list of triangles
            Triangles.Add(superTriangle);

            // Iterate through each point and incrementally add them to the triangulation
            foreach (Vector3 point in points)
            {
                List<Triangle> badTriangles = new List<Triangle>();
                foreach (Triangle triangle in Triangles)
                {
                    if (triangle.CircumcircleContains(point))
                    {
                        badTriangles.Add(triangle);
                    }
                }

                List<Edge> polygon = new List<Edge>();
                foreach (Triangle triangle in badTriangles)
                {
                    foreach (Edge edge in triangle.Edges)
                    {
                        bool shared = false;
                        foreach (Triangle otherTriangle in badTriangles)
                        {
                            if (otherTriangle != triangle && otherTriangle.ContainsEdge(edge))
                            {
                                shared = true;
                                break;
                            }
                        }
                        if (!shared)
                        {
                            polygon.Add(edge);
                        }
                    }
                }

                foreach (Triangle triangle in badTriangles)
                {
                    Triangles.Remove(triangle);
                }

                foreach (Edge edge in polygon)
                {
                    Triangles.Add(new Triangle(edge.Point1, edge.Point2, point));
                }
            }

            // Remove triangles that contain any of the super triangle vertices
            Triangles.RemoveAll(triangle =>
                triangle.Contains(superTriangle.Points[0]) ||
                triangle.Contains(superTriangle.Points[1]) ||
                triangle.Contains(superTriangle.Points[2])
            );
        }
    }

    public class Triangle
    {
        public Vector3[] Points { get; private set; }
        public Edge[] Edges { get; private set; }

        public Triangle(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            Points = new Vector3[] { point1, point2, point3 };
            Edges = new Edge[]
            {
            new Edge(point1, point2),
            new Edge(point2, point3),
            new Edge(point3, point1)
            };
        }

        public bool Contains(Vector3 point)
        {
            Vector3 p1 = Points[0], p2 = Points[1], p3 = Points[2];
            float area = 0.5f * (-p2.z * p3.x + p1.z * (-p2.x + p3.x) + p1.x * (p2.z - p3.z) + p2.x * p3.z);
            float s = 1 / (2 * area) * (p1.z * p3.x - p1.x * p3.z + (p3.z - p1.z) * point.x + (p1.x - p3.x) * point.z);
            float t = 1 / (2 * area) * (p1.x * p2.z - p1.z * p2.x + (p1.z - p2.z) * point.x + (p2.x - p1.x) * point.z);
            return s >= 0 && t >= 0 && (s + t) <= 1;
        }

        public bool CircumcircleContains(Vector3 point)
        {
            Vector3 p1 = Points[0], p2 = Points[1], p3 = Points[2];
            float ax = p1.x - point.x;
            float ay = p1.y - point.y;
            float bx = p2.x - point.x;
            float by = p2.y - point.y;
            float cx = p3.x - point.x;
            float cy = p3.y - point.y;
            float ap = ax * ax + ay * ay;
            float bp = bx * bx + by * by;
            float cp = cx * cx + cy * cy;
            return (ax * (by * cp - bp * cy) - ay * (bx * cp - bp * cx) + ap * (bx * cy - by * cx)) > 0;
        }

        public bool ContainsEdge(Edge edge)
        {
            foreach (Edge e in Edges)
            {
                if (e.Equals(edge))
                    return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"Triangle: [{Points[0]}, {Points[1]}, {Points[2]}]";
        }
    }

    public class Edge
    {
        public Vector3 Point1 { get; private set; }
        public Vector3 Point2 { get; private set; }

        public Edge(Vector3 point1, Vector3 point2)
        {
            Point1 = point1;
            Point2 = point2;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Edge)) return false;
            Edge otherEdge = (Edge)obj;
            return (Point1.Equals(otherEdge.Point1) && Point2.Equals(otherEdge.Point2)) ||
                   (Point1.Equals(otherEdge.Point2) && Point2.Equals(otherEdge.Point1));
        }

        public override int GetHashCode()
        {
            return Point1.GetHashCode() ^ Point2.GetHashCode();
        }
    }

}