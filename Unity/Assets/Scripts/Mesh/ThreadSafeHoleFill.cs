using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThreadSafeHoleFill : MonoBehaviour {

    public MeshInfo FillHoleSimple(List<Vector3> edges, Fragment colour) {

        float minX, minY, minZ;
        minX = minY = minZ = float.MaxValue;
        float maxX, maxY, maxZ;
        maxX = maxY = maxZ = float.MinValue;

        foreach (Vector3 v in edges) {
            minX = Mathf.Min(minX, v.x);
            minY = Mathf.Min(minY, v.y);
            minZ = Mathf.Min(minZ, v.z);

            maxX = Mathf.Max(maxX, v.x);
            maxY = Mathf.Max(maxY, v.y);
            maxZ = Mathf.Max(maxZ, v.z);
        }

        Vector3 mid = new Vector3((maxX-minX)/2, (maxY-minY)/2, (maxZ-minZ)/2);
        mid = Vector3.zero;

        return FillHoleSimple(edges, colour, mid);
    }

    public MeshInfo FillHoleSimple(List<Vector3> edges, Fragment colour, Vector3 mid) {

        List<int> indices = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        Dictionary<string, int> vertexIndices = new Dictionary<string, int>();

        vertices.Add(mid);

        for (int i = 0; i < edges.Count; i += 3) {
            Vector3 v1 = edges[i];
            Vector3 v2 = edges[i + 1];
            Vector3 v3 = edges[i + 2];

            int found;

            if (v1.x == float.PositiveInfinity) {
                indices.Add(0);
            } else {
                string s1 = v1.x + "," + v1.y + "," + v1.z;
                if (vertexIndices.TryGetValue(s1, out found)) {
                    indices.Add(found);
                } else {
                    found = vertices.Count;
                    indices.Add(found);
                    vertices.Add(v1);
                    vertexIndices.Add(s1, found);
                }
            }

            if (v2.x == float.PositiveInfinity) {
                indices.Add(0);
            } else {
                string s2 = v2.x + "," + v2.y + "," + v2.z;
                if (vertexIndices.TryGetValue(s2, out found)) {
                    indices.Add(found);
                } else {
                    found = vertices.Count;
                    indices.Add(found);
                    vertices.Add(v2);
                    vertexIndices.Add(s2, found);
                }
            }

            if (v3.x == float.PositiveInfinity) {
                indices.Add(0);
            } else {
                string s3 = v3.x + "," + v3.y + "," + v3.z;
                if (vertexIndices.TryGetValue(s3, out found)) {
                    indices.Add(found);
                } else {
                    found = vertices.Count;
                    indices.Add(found);
                    vertices.Add(v3);
                    vertexIndices.Add(s3, found);
                }
            }
        }

        return new MeshInfo(vertices.ToArray(), indices.ToArray(), colour);
    }

    public MeshInfo Stitch(List<Vector3> edges, Fragment colour, List<Vector3> solid) {
        KDTree tree = KDTree.MakeFromPoints(solid.ToArray());

        List<int> indices = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        Dictionary<string, int> vertexIndices = new Dictionary<string, int>();

        for (int i = 0; i < edges.Count; i += 3) {
            Vector3 v1 = edges[i];
            Vector3 v2 = edges[i + 1];
            Vector3 v3 = edges[i + 2];

            int found;

            if (v1.x == float.PositiveInfinity) {
                Vector3 voxel = solid[tree.FindNearest(v2)];
                indices.Add(vertices.Count);
                vertices.Add(voxel);
            } else {
                string s1 = v1.x + "," + v1.y + "," + v1.z;
                if (vertexIndices.TryGetValue(s1, out found)) {
                    indices.Add(found);
                } else {
                    found = vertices.Count;
                    indices.Add(found);
                    vertices.Add(v1);
                    vertexIndices.Add(s1, found);
                }
            }

            if (v2.x == float.PositiveInfinity) {
                Vector3 voxel = solid[tree.FindNearest(v1)];
                indices.Add(vertices.Count);
                vertices.Add(voxel);
            } else {
                string s2 = v2.x + "," + v2.y + "," + v2.z;
                if (vertexIndices.TryGetValue(s2, out found)) {
                    indices.Add(found);
                } else {
                    found = vertices.Count;
                    indices.Add(found);
                    vertices.Add(v2);
                    vertexIndices.Add(s2, found);
                }
            }

            if (v3.x == float.PositiveInfinity) {
                Vector3 voxel = solid[tree.FindNearest(v1)];
                indices.Add(vertices.Count);
                vertices.Add(voxel);
            } else {
                string s3 = v3.x + "," + v3.y + "," + v3.z;
                if (vertexIndices.TryGetValue(s3, out found)) {
                    indices.Add(found);
                } else {
                    found = vertices.Count;
                    indices.Add(found);
                    vertices.Add(v3);
                    vertexIndices.Add(s3, found);
                }
            }
        }

        return new MeshInfo(vertices.ToArray(), indices.ToArray(), colour);
    }
}
