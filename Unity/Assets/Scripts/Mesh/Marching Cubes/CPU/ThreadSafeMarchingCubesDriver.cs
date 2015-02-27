using UnityEngine;
using System.Collections;

public class ThreadSafeMarchingCubesDriver {

    public MeshInfo StartMarching(short[, ,] colouredVoxels, Fragment minMax, ThreadSafeVoxelisation.Voxelization.AABCGrid grid) {

        PrepareMarch();

        float[, ,] voxels = new float[colouredVoxels.GetLength(0), colouredVoxels.GetLength(1), colouredVoxels.GetLength(2)];

        PrepareGrid(colouredVoxels, voxels, minMax);

        MeshInfo mesh = ThreadSafeMarchingCubes.CreateMesh(voxels, minMax, grid);

        return mesh;

    }

    public MeshInfo StartMarchingClamp(short[, ,] colouredVoxels, Fragment minMax, ThreadSafeVoxelisation.Voxelization.AABCGrid grid, KDTree surface, Vector3[] parentVerts) {

        PrepareMarch();

        float[, ,] voxels = new float[colouredVoxels.GetLength(0), colouredVoxels.GetLength(1), colouredVoxels.GetLength(2)];

        PrepareGrid(colouredVoxels, voxels, minMax);

        foreach (Vector3 v in minMax.vertices) {
            voxels[(int)v.x, (int)v.y, (int)v.z] = -999f;
        }

        MeshInfo mesh = ThreadSafeMarchingCubes.CreateMeshClamp(voxels, minMax, grid, surface, parentVerts);

        return mesh;

    }

    public void PrepareMarch() {
        //Target is the value that represents the surface of mesh
        //For example the perlin noise has a range of -1 to 1 so the mid point is were we want the surface to cut through
        //The target value does not have to be the mid point it can be any value with in the range
        ThreadSafeMarchingCubes.SetTarget(0.0f);

        //Winding order of triangles use 2,1,0 or 0,1,2
        ThreadSafeMarchingCubes.SetWindingOrder(2, 1, 0);

        //Set the mode used to create the mesh
        //Cubes is faster and creates less verts, tetrahedrons is slower and creates more verts but better represents the mesh surface
        //MarchingCubes.SetModeToCubes();
        ThreadSafeMarchingCubes.SetModeToTetrahedrons();
    }

    public void PrepareGrid(short[, ,] colouredVoxels, float[, ,] voxels, Fragment minMax) {
        //Fill voxels with values. Im using perlin noise but any method to create voxels will work
        for (short x = (short)(minMax.minX - 2); x <= minMax.maxX + 2; x++) {
            for (short y = (short)(minMax.minY - 2); y <= minMax.maxY + 2; y++) {
                for (short z = (short)(minMax.minZ - 2); z <= minMax.maxZ + 2; z++) {
                    if (x < 0 || y < 0 || z < 0) continue;
                    if (x >= colouredVoxels.GetLength(0) || y >= colouredVoxels.GetLength(1) || z >= colouredVoxels.GetLength(2)) continue;
                    if (colouredVoxels[x, y, z] == minMax.colour) {
                        voxels[x, y, z] = -1f;
                    } else {
                        voxels[x, y, z] = 1f;
                    }
                }
            }
        }
    }

}
