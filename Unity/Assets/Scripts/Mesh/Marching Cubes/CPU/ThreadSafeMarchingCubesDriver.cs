using UnityEngine;
using System.Collections;

public class ThreadedMarchingCubesDriver {

    public MeshInfo StartMarching(short[, ,] colouredVoxels, Colouring minMax, ThreadedVoxelisation.Voxelization.AABCGrid grid, KDTree surface, Vector3[] parentVerts) {

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
        for (short x = (short)(minMax.minX-2); x <= minMax.maxX+2; x++) {
            for (short y = (short)(minMax.minY-2); y <= minMax.maxY+2; y++) {
                for (short z = (short)(minMax.minZ-2); z <= minMax.maxZ+2; z++) {
                    if (x < 0 || y < 0 || z < 0) continue;
                    if (x >= colouredVoxels.GetLength(0) || y >= colouredVoxels.GetLength(1) || z >= colouredVoxels.GetLength(2)) continue;
                    if (colouredVoxels[x,y,z] == minMax.colour) {
                        voxels[x, y, z] = -1f;
                    } else {
                        voxels[x, y, z] = 1f;
                    }
                }
            }
        }

        if (surface != null) {
            foreach (Vertex3 v in minMax.vertices) {
                voxels[(int)v.x, (int)v.y, (int)v.z] = -999f;
            }
        }

        /*for (short x = 0; x < colouredVoxels.GetLength(0); x++) {
            for (short y = 0; y < colouredVoxels.GetLength(1); y++) {
                for (short z = 0; z < colouredVoxels.GetLength(2); z++) {
                    if (colouredVoxels[x, y, z] == minMax.colour) {
                        voxels[x, y, z] = -1f;
                    } else {
                        voxels[x, y, z] = 1f;
                    }
                }
            }
        }*/

        MeshInfo mesh = ThreadedMarchingCubes.CreateMesh(voxels, minMax, grid, surface, parentVerts);

        return mesh;

    }

}
