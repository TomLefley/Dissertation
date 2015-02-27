using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ConstructiveSolidGeometry {


    public MeshInfo StartMeshing(MeshInfo mesh, MeshInfo minus, Fragment body, List<Fragment> fragments, short[,,] voxels, ThreadSafeVoxelisation.GridSize size) {

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

            bool b1 = vertSet(toVoxelSpace(v1, size, 0.5f), body, fragments, voxels);
            bool b2 = vertSet(toVoxelSpace(v2, size, 0.5f), body, fragments, voxels);
            bool b3 = vertSet(toVoxelSpace(v3, size, 0.5f), body, fragments, voxels);

            if (!b1 && !b2 && !b3) {
                StoreVertex(v1, foundIndices, indices, vertices);
                StoreVertex(v2, foundIndices, indices, vertices);
                StoreVertex(v3, foundIndices, indices, vertices);
            }

        }

        tris = minus.index;
        verts = minus.verts;

        for (int i = 0; i < tris.Length; i += 3) {

            Vector3 v1 = verts[tris[i]];
            Vector3 v2 = verts[tris[i + 1]];
            Vector3 v3 = verts[tris[i + 2]];

            bool b1 = vertInside(toVoxelSpace(v1, size, 1f), body, fragments, voxels);
            bool b2 = vertInside(toVoxelSpace(v2, size, 1f), body, fragments, voxels);
            bool b3 = vertInside(toVoxelSpace(v3, size, 1f), body, fragments, voxels);

            if (b1 && b2 && b3) {
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

    private Vector3 toVoxelSpace(Vector3 worldSpace, ThreadSafeVoxelisation.GridSize size, float scale) {
        float side = size.side;

        Vector3 mid = new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);

        return new Vector3(worldSpace.x * size.x * scale, worldSpace.y * size.y * scale, worldSpace.z * size.z * scale) +mid;

        
    }

    private bool vertSet(Vector3 grid, Fragment body, List<Fragment> fragments, short[, ,] voxels) {
        if (grid.x < 0 || grid.y < 0 || grid.z < 0) {
            return false;
        }
        if (grid.x >= voxels.GetLength(0) || grid.y >= voxels.GetLength(1) || grid.z >= voxels.GetLength(2)) {
            return false;
        }

        int x = (int)(grid.x);
        int y = (int)(grid.y);
        int z = (int)(grid.z);

        short c = voxels[x,y,z];
        int r = 2;

        if (c == 0) {
            for (int i = -r; i <= r; i++) {
                for (int j = -r; j <= r; j++) {
                    for (int k = -r; k <= r; k++) {
                        if (x + i < 0 || y + j < 0 || z + k < 0) {
                            continue;
                        }
                        if (x + i >= voxels.GetLength(0) || y + j >= voxels.GetLength(1) || k + z >= voxels.GetLength(2)) {
                            continue;
                        }

                        c = (short)Mathf.Max(c, voxels[x + i, y + j, z + k]);
                    }
                }
            }
        }

        if (c == 0) return false;
        if (c != body.colour) return true;

        return false;
    }

    private bool vertInside(Vector3 grid, Fragment body, List<Fragment> fragments, short[, ,] voxels) {
        return true;
        if (grid.x < 0 || grid.y < 0 || grid.z < 0) {
            return false;
        }
        if (grid.x >= voxels.GetLength(0) || grid.y >= voxels.GetLength(1) || grid.z >= voxels.GetLength(2)) {
            return false;
        }

        int x = (int)(grid.x);
        int y = (int)(grid.y);
        int z = (int)(grid.z);

        if (voxels[x, y, z] == 0) return false;

        return true;
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
