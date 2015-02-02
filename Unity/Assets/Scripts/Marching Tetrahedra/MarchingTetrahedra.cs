// Conversion by mgear @ http://unitycoder.com/blog/2012/11/12/marching-tetrahedrons/
// Bug fixes by alia

// Just attach to a renderable game object.
// Go grab testdata.js from
// https://github.com/mikolalysenko/mikolalysenko.github.com/tree/master/Isosurface/js
// for more interesting shapes.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MarchingTetrahedra : MonoBehaviour {
 
    int[] res = new int[3];
    private float[] grid = new float[8];
    private int[][] cube_vertices = new int[][]{ new int[]{0,0,0}
								    , new int[]{1,0,0}
								    , new int[]{1,1,0}
								    , new int[]{0,1,0}
								    , new int[]{0,0,1}
								    , new int[]{1,0,1}
								    , new int[]{1,1,1}
								    , new int[]{0,1,1} };
 
    private int[]  x = new int[3];
    private List<Vector3> vertices = new List<Vector3>();
    private List<int[]> faces = new List<int[] >();

    private Dictionary<string, int> indices = new Dictionary<string, int>();
 
    void Awake ()
    {
	    x[0]=0;
	    x[1]=0;
	    x[2]=0;
    }
 
    public void StartMarching (float[,,] voxels)
    {

        res[0] = voxels.GetLength(0);
        res[1] = voxels.GetLength(1);
        res[2] = voxels.GetLength(2);

        Debug.Log(Time.realtimeSinceStartup);

        Vector3[] verts = MarchingTetrahedrons(voxels, res);
	
	    List<int> final_faces = new List<int>();
	
	    for (var n=0;n<faces.Count;n++)
	    {
		    if (faces[n].Length == 3)
		    {
			    // Triangle 1
			    final_faces.AddRange(faces[n]);
		    }
		    else
		    {
			    // Triangle 1
			    final_faces.Add(faces[n][0]);
			    final_faces.Add(faces[n][1]);
			    final_faces.Add(faces[n][2]);
			
			    // Triangle 2
			    final_faces.Add(faces[n][2]);
			    final_faces.Add(faces[n][3]);
			    final_faces.Add(faces[n][0]);
		    }
	    }
	
	    int[] triangles = final_faces.ToArray();

        Debug.Log(Time.realtimeSinceStartup);
	 	
	    Mesh mesh = GetComponent<MeshFilter>().mesh;
	    mesh.vertices = verts;
	    mesh.triangles = triangles;
	    mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
 
    /**
    * Marching Tetrahedra in Javascript
    *
    * Based on Paul Bourke's implementation
    *  http://local.wasp.uwa.edu.au/~pbourke/geometry/polygonise/
    *
    * (Several bug fixes were made to deal with oriented faces)
    *
    * Javascript port by Mikola Lysenko : http://0fps.wordpress.com/
    */
    Vector3[] MarchingTetrahedrons(float[,,] data, int[] dims)
    {
	    int[][] tetra_list =  new int[][]{    new int[]{0,2,3,7}
						    , new int[]{0,6,2,7}
						    , new int[]{0,4,6,7}
						    , new int[]{0,6,1,2}
						    , new int[]{0,1,6,4}
						    , new int[]{5,6,1,4} };
	 
	    int n = 0;
	    int[] edges = new int[12];
	 
	    //March over the volume
	    for (x[2]=0; x[2]<dims[2]-1; x[2]++)
	    {
		    for(x[1]=0; x[1]<dims[1]-1; x[1]++)
		    {
			    for(x[0]=0; x[0]<dims[0]-1; x[0]++)
			    {
				    //Read in cube
				    for(var i=0; i<8; ++i)
				    {
					    //grid[i] = data[n + cube_vertices[i][0] + dims[0] * (cube_vertices[i][1] + dims[1] * cube_vertices[i][2])];
                        grid[i] = data[x[0] + cube_vertices[i][0], x[1] + cube_vertices[i][1], x[2] + cube_vertices[i][2]];
				    }
				 
				    for(int i=0; i<tetra_list.Length; ++i)
				    {
					    int[] T = tetra_list[i];
					    int triindex = 0;
					    if (grid[T[0]] < 0) triindex |= 1;
					    if (grid[T[1]] < 0) triindex |= 2;
					    if (grid[T[2]] < 0) triindex |= 4;
					    if (grid[T[3]] < 0) triindex |= 8;
					 
					    //Handle each case
					    switch (triindex)
					    {
					    case 0x00:
					    case 0x0F:
						    break;
					    case 0x0E:
						    faces.Add(new int[]{interp(T[0], T[1]), interp(T[0], T[3]), interp(T[0],T[2])});
						    break;
					    case 0x01:
						    faces.Add(new int[]{interp(T[0], T[1]), interp(T[0], T[2]), interp(T[0], T[3])});
						    break;
					    case 0x0D:
						    faces.Add(new int[]{interp(T[1], T[0]), interp(T[1], T[2]), interp(T[1], T[3])});
						    break;
					    case 0x02:
						    faces.Add(new int[]{interp(T[1], T[0]), interp(T[1], T[3]), interp(T[1], T[2])});
						    break;
					    case 0x0C:
						    faces.Add(new int[]{interp(T[1], T[2]), interp(T[1], T[3]), interp(T[0], T[3]), interp(T[0], T[2])});
						    break;
					    case 0x03:
						    faces.Add(new int[]{interp(T[1], T[2]), interp(T[0], T[2]), interp(T[0], T[3]), interp(T[1], T[3])});
						    break;
					    case 0x04:
						    faces.Add(new int[]{interp(T[2], T[0]), interp(T[2], T[1]), interp(T[2], T[3])});
						    break;
					    case 0x0B:
						    faces.Add(new int[]{interp(T[2], T[0]), interp(T[2], T[3]), interp(T[2], T[1])});
						    break;
					    case 0x05:
						    faces.Add(new int[]{interp(T[0], T[1]), interp(T[1], T[2]), interp(T[2], T[3]), interp(T[0], T[3])});
						    break;
					    case 0x0A:
						    faces.Add(new int[]{interp(T[0], T[1]), interp(T[0], T[3]), interp(T[2], T[3]), interp(T[1], T[2])});
						    break;
					    case 0x06:
						    faces.Add(new int[]{interp(T[2], T[3]), interp(T[0], T[2]), interp(T[0], T[1]), interp(T[1], T[3])});
						    break;
					    case 0x09:
						    faces.Add(new int[]{interp(T[2], T[3]), interp(T[1], T[3]), interp(T[0], T[1]), interp(T[0], T[2])});
						    break;
					    case 0x07:
						    faces.Add(new int[]{interp(T[3], T[0]), interp(T[3], T[1]), interp(T[3], T[2])});
						    break;
					    case 0x08:
						    faces.Add(new int[]{interp(T[3], T[0]), interp(T[3], T[2]), interp(T[3], T[1])});
						    break;
					    }
				    }
				    ++n;
			    }
			    ++n;
		    }
		    n+=dims[0];
	    }
	 
	    return vertices.ToArray();
    }
 
    int interp(int i0, int i1)
    {
	    var g0 = grid[i0];
	    var g1 = grid[i1];
	    var p0 = cube_vertices[i0];
	    var p1 = cube_vertices[i1];
	    float x0 = x[0];
	    float x1 = x[1];
	    float x2 = x[2];
	    float[] v  = new float[]{ x0, x1, x2};
	    var t = g0 - g1;
	 
	    if(Mathf.Abs(t) > 1e-6) 
	    {
		    t = g0 / t;
	    }
	    for(var i=0; i<3; ++i) 
	    {
		    v[i] += p0[i] + t * (p1[i] - p0[i]);
	    }

        int index;
        bool found = indices.TryGetValue(v[0]+""+v[1]+""+v[2], out index);
        if (found) {
            return index;
        } else {
            vertices.Add(new Vector3(v[0], v[1], v[2]));
            indices.Add(v[0] + "" + v[1] + "" + v[2], vertices.Count - 1);
            return vertices.Count - 1;
        }
	    
    }
 
    float[,,] makeVolume()
    {
 
	    //var dims = [[-1.0, 1.0, 0.25], 
	    //			[-1.0, 1.0, 0.25], 
	    //			[-1.0, 1.0, 0.25]];
	    float[][] dims = new float[][]{new float[]{-2.0f, 2.0f, 0.2f},
			         new float[]{-2.0f, 2.0f, 0.2f},
                     new float[]{-1.0f, 1.0f, 0.2f}};
				
	    for(var i1=0; i1<3; ++i1) 
	    {
		    res[i1] = (int)(2 + Mathf.Ceil((dims[i1][1] - dims[i1][0]) / dims[i1][2]));
	    }
	
	    float[,,] volume = new float[res[0], res[1], res[2]];
	    int n = 0;
	    float zz = dims[2][0]-dims[2][2];
	    for(int k=0; k<res[2]; k++) //z
	    {
		    float yy = dims[1][0]-dims[1][2];
		    for(int j=0; j<res[1]; j++) // y
		    {
			    float xx = dims[0][0]-dims[0][2];
			    for(int i=0; i<res[0]; i++) // x
			    {
				    volume[i,j,k] = makeSphere(xx,yy,zz);
				    xx+=dims[0][2];
				    ++n;
			    }
			    yy+=dims[1][2];
		    }
		    zz+=dims[2][2];
	    }
	    return volume;
    }
 
    float makeSphere(float x, float y, float z)
    {
	    return x*x + y*y + z*z - 1.0f;//Mathf.Pow(x, 2.0f)+Mathf.Pow(y, 2.0f)+Mathf.Pow(z, 2.0f)-1.0f;//
    }

    float makeTorus(float x, float y, float z)
    {
        return Mathf.Pow(1.0f - Mathf.Sqrt(Mathf.Pow(x, 2.0f) + Mathf.Pow(y, 2.0f)), 2) + Mathf.Pow(z, 2.0f) - 0.25f;
    }
}