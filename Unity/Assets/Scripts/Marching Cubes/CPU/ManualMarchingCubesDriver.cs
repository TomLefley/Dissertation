using UnityEngine;
using System.Collections;

public class ManualMarchingCubesDriver : MonoBehaviour {

public Material m_material;

private GameObject m_mesh;

public void StartMarching (ManualVoxelisation.Voxelization.AABCGrid grid) 
{
	
	//Target is the value that represents the surface of mesh
	//For example the perlin noise has a range of -1 to 1 so the mid point is were we want the surface to cut through
	//The target value does not have to be the mid point it can be any value with in the range
	ManualMarchingCubes.SetTarget(0.0f);
	
	//Winding order of triangles use 2,1,0 or 0,1,2
    ManualMarchingCubes.SetWindingOrder(2, 1, 0);
	
	//Set the mode used to create the mesh
	//Cubes is faster and creates less verts, tetrahedrons is slower and creates more verts but better represents the mesh surface
	//MarchingCubes.SetModeToCubes();
    ManualMarchingCubes.SetModeToTetrahedrons();
	
	//The size of voxel array. Be carefull not to make it to large as a mesh in unity can only be made up of 65000 verts
    ManualVoxelisation.GridSize size = grid.GetSize();

    int width = size.x;
    int height = size.y;
    int length = size.z;
	
	float[,,] voxels = new float[width, height, length];
	
	//Fill voxels with values. Im using perlin noise but any method to create voxels will work
	for(short x = 0; x < width; x++)
	{
		for(short y = 0; y < height; y++)
		{
			for(short z = 0; z < length; z++)
			{
                if (grid.IsAABCSet(x, y, z)) {
                    voxels[x, y, z] = -1f;
                } else {
                    voxels[x, y, z] = 1f;
                }
			}
		}
	}

    Mesh mesh = ManualMarchingCubes.CreateMesh(voxels, grid);
	
	//The diffuse shader wants uvs so just fill with a empty array, there not actually used
	mesh.uv = new Vector2[mesh.vertices.Length];
	mesh.RecalculateNormals();
	
	/*m_mesh = new GameObject("Mesh");
	m_mesh.AddComponent<MeshFilter>();
	m_mesh.AddComponent<MeshRenderer>();
	m_mesh.renderer.material = m_material;
	m_mesh.GetComponent<MeshFilter>().mesh = mesh;
	//Center mesh
	m_mesh.transform.localPosition = new Vector3(-width/2, -height/2, -length/2);*/

    gameObject.GetComponent<MeshFilter>().mesh = mesh;
    //gameObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
    //gameObject.transform.Translate(new Vector3(-1, -1, -1));
	
}

}
