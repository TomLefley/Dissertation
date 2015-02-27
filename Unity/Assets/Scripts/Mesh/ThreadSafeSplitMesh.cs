using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThreadSafeSplitMesh {

    Dictionary<string, short> mappings = new Dictionary<string, short>();

    public Dictionary<short, MeshInfo> Split(MeshInfo mesh, KDTree tree, List<Vector3> voxels, short[,,] colourings, ThreadSafeVoxelisation.GridSize size) {

        Dictionary<short, Dictionary<string, int>> meshFoundIndices = new Dictionary<short, Dictionary<string, int>>();
        Dictionary<short, List<Vector3>> meshVertices = new Dictionary<short, List<Vector3>>();
        Dictionary<short, List<int>> meshIndices = new Dictionary<short, List<int>>();
        Dictionary<short, List<Vector3>> meshEdges = new Dictionary<short, List<Vector3>>();


        int[] tris = mesh.index;
        Vector3[] verts = mesh.verts;

        for (int i = 0; i < tris.Length; i += 3) {

            Vector3 v1 = verts[tris[i]];
            Vector3 v2 = verts[tris[i + 1]];
            Vector3 v3 = verts[tris[i + 2]];

            short c1 = nearestColour(toVoxelSpace(v1, size, 0.5f), tree, voxels, colourings);
            short c2 = nearestColour(toVoxelSpace(v2, size, 0.5f), tree, voxels, colourings);
            short c3 = nearestColour(toVoxelSpace(v3, size, 0.5f), tree, voxels, colourings);

            if (c1 == c2 && c1 == c3) {
                Dictionary<string, int> foundIndices;
                if (!(meshFoundIndices.TryGetValue(c1, out foundIndices))) {
                    foundIndices = new Dictionary<string, int>();
                    meshFoundIndices.Add(c1, foundIndices);
                }

                List<Vector3> vertices;
                if (!(meshVertices.TryGetValue(c1, out vertices))) {
                    vertices = new List<Vector3>();
                    meshVertices.Add(c1, vertices);
                }

                List<int> indices;
                if (!(meshIndices.TryGetValue(c1, out indices))) {
                    indices = new List<int>();
                    meshIndices.Add(c1, indices);
                }

                StoreVertex(v1, foundIndices, indices, vertices);
                StoreVertex(v2, foundIndices, indices, vertices);
                StoreVertex(v3, foundIndices, indices, vertices);

            } else {
                Vector3 mid = Vector3.zero;
                if (c1 == c2) {
                    List<Vector3> edges;
                    if (!(meshEdges.TryGetValue(c1, out edges))) {
                        edges = new List<Vector3>();
                        meshEdges.Add(c1, edges);
                    }
                    edges.Add(v1);
                    edges.Add(v2);
                }
                if (c1 == c3) {
                    List<Vector3> edges;
                    if (!(meshEdges.TryGetValue(c1, out edges))) {
                        edges = new List<Vector3>();
                        meshEdges.Add(c1, edges);
                    }
                    edges.Add(v1);
                    edges.Add(v3);
                }
                if (c2 == c3) {
                    List<Vector3> edges;
                    if (!(meshEdges.TryGetValue(c1, out edges))) {
                        edges = new List<Vector3>();
                        meshEdges.Add(c1, edges);
                    }
                    edges.Add(v2);
                    edges.Add(v3);
                }
            }

        }

        Dictionary<short, MeshInfo> output = new Dictionary<short, MeshInfo>();

        foreach (short s in meshFoundIndices.Keys) {
            List<Vector3> vertices;
            meshVertices.TryGetValue(s, out vertices);

            List<int> indices;
            meshIndices.TryGetValue(s, out indices);

            List<Vector3> edges;
            meshEdges.TryGetValue(s, out edges);

            Fragment c = new Fragment(s);

            c.vertices.AddRange(edges);

            output.Add(s, new MeshInfo(vertices.ToArray(), indices.ToArray(), c));

        }

        return output;
    }

    private Vector3 toVoxelSpace(Vector3 worldSpace, ThreadSafeVoxelisation.GridSize size, float scale) {
        float side = size.side;

        Vector3 mid = new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);

        return new Vector3(worldSpace.x * size.x * scale, worldSpace.y * size.y * scale, worldSpace.z * size.z * scale) + mid;


    }

    private short nearestColour(Vector3 vert, KDTree tree, List<Vector3> voxels, short[,,] colourings) {
        short colour;
        if (mappings.TryGetValue(vert.x+","+vert.y+","+vert.z, out colour)) {
            return colour;
        } else {   
            int index = tree.FindNearest(vert);
            Vector3 voxel = voxels[index];
            mappings.Add(vert.x+","+vert.y+","+vert.z, colourings[(int)voxel.x, (int)voxel.y, (int)voxel.z]);
            return colourings[(int)voxel.x, (int)voxel.y, (int)voxel.z];
        }
        
    }

    void StoreVertex(Vector3 vertex, Dictionary<string, int> foundIndices, List<int> indices, List<Vector3> vertices) {
        int idx = vertices.Count;

        string points = vertex.x + "," + vertex.y + "," + vertex.z;
        int index = -1;

        bool found = foundIndices.TryGetValue(points, out index);

        if (found) {
            indices.Add(index);
        } else {
            indices.Add(idx);
            vertices.Add(vertex);
            foundIndices.Add(points, idx);
        }
    }
}
