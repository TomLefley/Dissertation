using UnityEngine;
using System.Collections;

public class ThreadedMarchingCubesDriver {

    public MeshInfo StartMarching(float[, ,] colouredVoxels, Colouring minMax, ThreadedVoxelisation.Voxelization.AABCGrid grid) {

        //Target is the value that represents the surface of mesh
        //For example the perlin noise has a range of -1 to 1 so the mid point is were we want the surface to cut through
        //The target value does not have to be the mid point it can be any value with in the range
        ThreadedMarchingCubes.SetTarget(0.0f);

        //Winding order of triangles use 2,1,0 or 0,1,2
        ThreadedMarchingCubes.SetWindingOrder(2, 1, 0);

        //Set the mode used to create the mesh
        //Cubes is faster and creates less verts, tetrahedrons is slower and creates more verts but better represents the mesh surface
        //MarchingCubes.SetModeToCubes();
        ThreadedMarchingCubes.SetModeToTetrahedrons();

        //The size of voxel array. Be carefull not to make it to large as a mesh in unity can only be made up of 65000 verts
        ThreadedVoxelisation.GridSize size = grid.GetSize();

        float[, ,] voxels = new float[colouredVoxels.GetLength(0), colouredVoxels.GetLength(1), colouredVoxels.GetLength(2)];

        //Fill voxels with values. Im using perlin noise but any method to create voxels will work
        /*for (short x = minMax.minX; x <= minMax.maxX; x++) {
            for (short y = minMax.minY; y <= minMax.maxY; y++) {
                for (short z = minMax.minZ; z <= minMax.maxZ; z++) {
                    if (colouredVoxels[x,y,z] == minMax.colour) {
                        voxels[x, y, z] = -1f;
                    } else {
                        voxels[x, y, z] = 1f;
                    }
                }
            }
        }*/

        for (short x = 0; x < colouredVoxels.GetLength(0); x++) {
            for (short y = 0; y < colouredVoxels.GetLength(1); y++) {
                for (short z = 0; z < colouredVoxels.GetLength(2); z++) {
                    if (colouredVoxels[x, y, z] == minMax.colour) {
                        voxels[x, y, z] = -1f;
                    } else {
                        voxels[x, y, z] = 1f;
                    }
                }
            }
        }

        MeshInfo mesh = ThreadedMarchingCubes.CreateMesh(voxels, minMax, grid);

        return mesh;

    }

}
