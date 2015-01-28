using UnityEngine;
using System.Collections;

public class MarchingCubesDriver : MonoBehaviour {

    public Material m_material;

    private PerlinNoise m_perlin;
    private GameObject m_mesh;

    public void StartMarching(float[,,] colouredVoxels, MinMax minMax, Voxelisation.Voxelization.AABCGrid grid) {
        if (minMax.colour == 0) return;

        m_perlin = new PerlinNoise(2);

        //Target is the value that represents the surface of mesh
        //For example the perlin noise has a range of -1 to 1 so the mid point is were we want the surface to cut through
        //The target value does not have to be the mid point it can be any value with in the range
        MarchingCubes.SetTarget(0.0f);

        //Winding order of triangles use 2,1,0 or 0,1,2
        MarchingCubes.SetWindingOrder(2, 1, 0);

        //Set the mode used to create the mesh
        //Cubes is faster and creates less verts, tetrahedrons is slower and creates more verts but better represents the mesh surface
        //MarchingCubes.SetModeToCubes();
        MarchingCubes.SetModeToTetrahedrons();

        //The size of voxel array. Be carefull not to make it to large as a mesh in unity can only be made up of 65000 verts
        Voxelisation.GridSize size = grid.GetSize();

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
                        voxels[x, y, z] = 0f;
                    } else {
                        voxels[x, y, z] = 1f;
                    }
                }
            }
        }

        Mesh mesh = MarchingCubes.CreateMesh(voxels, minMax, grid);

        //The diffuse shader wants uvs so just fill with a empty array, there not actually used
        mesh.uv = new Vector2[mesh.vertices.Length];
        mesh.RecalculateNormals();

        if (minMax.colour == 1) {
            gameObject.GetComponent<MeshFilter>().mesh = mesh;
            gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
            transform.localScale = Vector3.one;
        } else {
            m_mesh = new GameObject("Fragment"+minMax.colour);
            m_mesh.AddComponent<MeshFilter>();
            m_mesh.AddComponent<MeshRenderer>();
            m_mesh.AddComponent<MeshCollider>();
            m_mesh.GetComponent<MeshCollider>().convex = true;
            m_mesh.AddComponent<Rigidbody>();
            m_mesh.rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            m_mesh.renderer.material = m_material;
            m_mesh.GetComponent<MeshCollider>().sharedMesh = mesh;
            m_mesh.GetComponent<MeshFilter>().mesh = mesh;
        }

    }

}
