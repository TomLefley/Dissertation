// Conversion by mgear @ http://unitycoder.com/blog/2012/11/12/marching-tetrahedrons/
// Bug fixes by alia

// Just attach to a renderable game object.
// Go grab testdata.js from
// https://github.com/mikolalysenko/mikolalysenko.github.com/tree/master/Isosurface/js
// for more interesting shapes.

import System.Collections.Generic;
 
var res = new Array(3);
private var grid:float[] = new float[8];
private var cube_vertices = 	[ [0,0,0]
								, [1,0,0]
								, [1,1,0]
								, [0,1,0]
								, [0,0,1]
								, [1,0,1]
								, [1,1,1]
								, [0,1,1] ];
 
private var  x:int[] = new int[3];
private var vertices:List.<Vector3> = new List.<Vector3>();
private var faces:List.<int[] > = new List.<int[] >();
 
function Awake ()
{
	x[0]=0;
	x[1]=0;
	x[2]=0;
}
 
function Start ()
{
	var verts:Vector3[] = MarchingTetrahedra(makeVolume(),res);
	
	var final_faces:List.<int> = new List.<int>();
	
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
	
	var triangles : int[] = final_faces.ToArray();
	 	
	var mesh : Mesh = GetComponent(MeshFilter).mesh;
	mesh.vertices = verts;
	mesh.triangles = triangles;
	mesh.RecalculateBounds();
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
function MarchingTetrahedra(data, dims)
{
	var tetra_list = 	[ [0,2,3,7]
						, [0,6,2,7]
						, [0,4,6,7]
						, [0,6,1,2]
						, [0,1,6,4]
						, [5,6,1,4] ];
	 
	var  n:int = 0;
	var  edges:int[] = new int[12];
	 
	//March over the volume
	for (x[2]=0; x[2]<dims[2]-1; ++x[2])
	{
		for(x[1]=0; x[1]<dims[1]-1; ++x[1])
		{
			for(x[0]=0; x[0]<dims[0]-1; ++x[0])
			{
				//Read in cube
				for(var i=0; i<8; ++i)
				{
					grid[i] = data[n + cube_vertices[i][0] + dims[0] * (cube_vertices[i][1] + dims[1] * cube_vertices[i][2])];
				}
				 
				for(i=0; i<tetra_list.length; ++i)
				{
					var T:int[] = tetra_list[i];
					var triindex:int = 0;
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
						faces.Add([interp(T[0], T[1]), interp(T[0], T[3]), interp(T[0],T[2])]);
						break;
					case 0x01:
						faces.Add([interp(T[0], T[1]), interp(T[0], T[2]), interp(T[0], T[3])]);
						break;
					case 0x0D:
						faces.Add([interp(T[1], T[0]), interp(T[1], T[2]), interp(T[1], T[3])]);
						break;
					case 0x02:
						faces.Add([interp(T[1], T[0]), interp(T[1], T[3]), interp(T[1], T[2])]);
						break;
					case 0x0C:
						faces.Add([interp(T[1], T[2]), interp(T[1], T[3]), interp(T[0], T[3]), interp(T[0], T[2])]);
						break;
					case 0x03:
						faces.Add([interp(T[1], T[2]), interp(T[0], T[2]), interp(T[0], T[3]), interp(T[1], T[3])]);
						break;
					case 0x04:
						faces.Add([interp(T[2], T[0]), interp(T[2], T[1]), interp(T[2], T[3])]);
						break;
					case 0x0B:
						faces.Add([interp(T[2], T[0]), interp(T[2], T[3]), interp(T[2], T[1])]);
						break;
					case 0x05:
						faces.Add([interp(T[0], T[1]), interp(T[1], T[2]), interp(T[2], T[3]), interp(T[0], T[3])]);
						break;
					case 0x0A:
						faces.Add([interp(T[0], T[1]), interp(T[0], T[3]), interp(T[2], T[3]), interp(T[1], T[2])]);
						break;
					case 0x06:
						faces.Add([interp(T[2], T[3]), interp(T[0], T[2]), interp(T[0], T[1]), interp(T[1], T[3])]);
						break;
					case 0x09:
						faces.Add([interp(T[2], T[3]), interp(T[1], T[3]), interp(T[0], T[1]), interp(T[0], T[2])]);
						break;
					case 0x07:
						faces.Add([interp(T[3], T[0]), interp(T[3], T[1]), interp(T[3], T[2])]);
						break;
					case 0x08:
						faces.Add([interp(T[3], T[0]), interp(T[3], T[2]), interp(T[3], T[1])]);
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
 
function interp(i0:float, i1:float)
{
	var g0 = grid[i0];
	var g1 = grid[i1];
	var p0 = cube_vertices[i0];
	var p1 = cube_vertices[i1];
	var x0:float = x[0];
	var x1:float = x[1];
	var x2:float = x[2];
	var v:float[]  = [ x0, x1, x2];
	var t = g0 - g1;
	 
	if(Mathf.Abs(t) > 1e-6) 
	{
		t = g0 / t;
	}
	for(var i=0; i<3; ++i) 
	{
		v[i] += p0[i] + t * (p1[i] - p0[i]);
	}
	vertices.Add(new Vector3(v[0],v[1],v[2]));
	return vertices.Count - 1;
}
 
function makeVolume()
{
 
	//var dims = [[-1.0, 1.0, 0.25], 
	//			[-1.0, 1.0, 0.25], 
	//			[-1.0, 1.0, 0.25]];
	var dims = [[-2.0, 2.0, 0.2],
			     [-2.0, 2.0, 0.2],
			     [-1.0, 1.0, 0.2]];
				
	for(var i1=0; i1<3; ++i1) 
	{
		res[i1] = 2 + Mathf.Ceil((dims[i1][1] - dims[i1][0]) / dims[i1][2]);
	}
	
	var volume = new Array(res[0] * res[1] * res[2]);
	var n:int = 0;
	var zz:float=dims[2][0]-dims[2][2];
	for(var k=0; k<res[2]; ++k) //z
	{
		var yy:float=dims[1][0]-dims[1][2];
		for(var j=0; j<res[1]; ++j) // y
		{
			var xx:float=dims[0][0]-dims[0][2];
			for(var i=0; i<res[0]; ++i) // x
			{
				volume[n] = makeSphere(xx,yy,zz);
				xx+=dims[0][2];
				++n;
			}
			yy+=dims[1][2];
		}
		zz+=dims[2][2];
	}
	return volume;
}
 
function makeSphere(x:float,y:float,z:float)
{
	return x*x + y*y + z*z - 1.0;//Mathf.Pow(x, 2.0f)+Mathf.Pow(y, 2.0f)+Mathf.Pow(z, 2.0f)-1.0f;//
}

function makeTorus(x:float,y:float,z:float)
{
    return Mathf.Pow(1.0 - Mathf.Sqrt(Mathf.Pow(x, 2.0f) + Mathf.Pow(y, 2.0f)), 2) + Mathf.Pow(z, 2.0f) - 0.25;
}