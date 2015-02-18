using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ConstructiveSolidGeometry {


    public MeshInfo StartMeshing(MeshInfo mesh, Colouring body, List<Colouring> fragments, short[,,] voxels, ThreadedVoxelisation.GridSize size) {

        MeshInfo output;

        int[] tris = mesh.index;
        Vector3[] verts = mesh.verts;

        Dictionary<string, int> foundIndices = new Dictionary<string, int>();
        List<int> indices = new List<int>();
        List<Vector3> vertices = new List<Vector3>();

        for (int i = 0; i < tris.Length; i += 3) {

            Vector3 v1 = verts[tris[i]];
            Vector3 v2 = verts[tris[i+1]];
            Vector3 v3 = verts[tris[i+2]];

            bool b1 = vertSet(toVoxelSpace(v1, size), body, fragments, voxels);
            bool b2 = vertSet(toVoxelSpace(v2, size), body, fragments, voxels);
            bool b3 = vertSet(toVoxelSpace(v3, size), body, fragments, voxels);

            if (!b1 && !b2 && !b3) {
                StoreVertex(v1, foundIndices, indices, vertices);
                StoreVertex(v2, foundIndices, indices, vertices);
                StoreVertex(v3, foundIndices, indices, vertices);
            }

        }

        tris = indices.ToArray();
        verts = vertices.ToArray();

        output = new MeshInfo(verts, tris, body);

        return output;
    }

    private Vector3 toVoxelSpace(Vector3 worldSpace, ThreadedVoxelisation.GridSize size) {
        float side = size.side;

        Vector3 mid = new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);

        return new Vector3(worldSpace.x * size.x * 0.5f, worldSpace.y * size.y * 0.5f, worldSpace.z * size.z * 0.5f) +mid;

        
    }

    private bool vertSet(Vector3 grid, Colouring body, List<Colouring> fragments, short[, ,] voxels) {
        if (grid.x < 0 || grid.y < 0 || grid.z < 0) {
            return false;
        }
        if (grid.x >= voxels.GetLength(0) || grid.y >= voxels.GetLength(1) || grid.z >= voxels.GetLength(2)) {
            return false;
        }

        int x = (int)(grid.x);
        int y = (int)(grid.y);
        int z = (int)(grid.z);

        foreach (Colouring colour in fragments) {
            if (colour.colour == body.colour) continue;
            short c;
            if (voxels[x, y, z] == 0) {
                short c1, c2, c3, c4, c5, c6;
                c1 = (x + 1 < voxels.GetLength(0)) ? voxels[x + 1, y, z] : (short)0;
                c2 = (x - 1 >=0) ? voxels[x - 1, y, z] : (short)0;
                c3 = (y + 1 < voxels.GetLength(1)) ? voxels[x, y + 1, z] : (short)0;
                c4 = (y - 1 >= 0) ? voxels[x, y - 1, z] : (short)0;
                c5 = (z + 1 < voxels.GetLength(2)) ? voxels[x, y , z + 1] : (short)0;
                c6 = (z - 1 >= 0) ? voxels[x, y, z - 1] : (short)0;

                c = (short)Mathf.Max(c1, Mathf.Max(c2, Mathf.Max(c3, Mathf.Max(c4, Mathf.Max(c5, c6)))));

                if (c == 0) continue;
            } else {
                c = voxels[x, y, z];
            }
            if (c == colour.colour) return true;
        }
        return false;
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
