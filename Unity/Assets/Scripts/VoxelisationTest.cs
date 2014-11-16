// Copyright (c) 2012 Vincenzo Giovanni Comito
// See the file license.txt for copying permission.

using System.Collections.Generic;
using UnityEngine;

namespace Voxelisation {
    public class VoxelisationTest : MonoBehaviour {

        public bool drawMeshShell = true;
        public bool drawMeshInside = true;
        public bool drawEmptyCube = false;
        public bool includeChildren = true;
        public bool createMultipleGrids = true;
        public Vector3 meshShellPositionFromObject = Vector3.zero;
        public float cubeSide = 0.1f;
        private List<Voxelisation.Voxelization.AABCGrid> aABCGrids;

        void Awake() {

        }

        void Start() {
            //	TestTriangleIntersectionAABC();
	        if(createMultipleGrids && includeChildren) {
		        aABCGrids = Voxelization.CreateMultipleGridsWithGameObjectMesh(gameObject, cubeSide, drawMeshInside);
	        } else {
		        Voxelization.AABCGrid thisObjGrid;
                aABCGrids = new List<Voxelisation.Voxelization.AABCGrid>();	
		        thisObjGrid = Voxelization.CreateGridWithGameObjectMesh(gameObject, cubeSide, includeChildren, drawMeshInside);
		        aABCGrids.Add(thisObjGrid);
	        }
        }

        void LateUpdate() {
        }

        void OnDrawGizmos() {
            Gizmos.color = new Color(1.0f, 0.0f, 0.0f, .5f);
            DrawMeshShell();
        }

        void TestTriangleIntersectionAABC() {
	        Vector3[] triangleA;
	        Vector3[] triangleB;
	        Vector3[] triangleC;
	        var aABCGrid = new Voxelization.AABCGrid(1, 1, 1, 1, new Vector3(1, 1, 1));
	        Vector3[] aABCVertices;
	
	        triangleA = new Vector3[3];
	        triangleA[0] = new Vector3(1, 1, 1);
	        triangleA[1] = new Vector3(1, 2, 1);
	        triangleA[2] = new Vector3(2, 1, 2);
	
	        triangleB = new Vector3[3];
	        triangleB[0] = new Vector3(2, 1, 1);
	        triangleB[1] = new Vector3(1, 2, 1);
	        triangleB[2] = new Vector3(1, 1, 2);
	
	        triangleC = new Vector3[3];
	        triangleC[0] = new Vector3(-1, -1, -2);
	        triangleC[1] = new Vector3(-1, -1, -2);
	        triangleC[2] = new Vector3(-1, -1, -2);
	
	        print("aabc vertices:");
	        aABCVertices = aABCGrid.GetAABCCorners(0, 0, 0);
	        for(var i = 0; i < 8; ++i) {
		        print("Vertex " + i + ": " + aABCVertices[i]);
	        } 
	
	        if(aABCGrid.TriangleIntersectAABC(triangleA, 0, 0, 0)) {
		        print("Triangle A intersect the AABC, Test is OK");
	        } else {
		        print("Triangle A doesn't intersect the AABC, Test is NOT OK");	
	        }
	        if(aABCGrid.TriangleIntersectAABC(triangleB, 0, 0, 0)) {
		        print("Triangle B intersect the AABC, Test is OK");
	        } else {
		        print("Triangle B doesn't intersect the AABC, Test is NOT OK");
	        }	
	        if(aABCGrid.TriangleIntersectAABC(triangleC, 0, 0, 0)) {
		        print("Triangle C intersect the AABC, Test is NOT OK");
	        } else {
		        print("Triangle C doesn't intersect the AABC, Test is OK");
	        }	
        }

        void DrawMeshShell() {
	        foreach(Voxelization.AABCGrid aABCGrid in aABCGrids) {
		        if(drawMeshShell && (aABCGrid!=null)) {
			        var cubeSize = new Vector3(cubeSide, cubeSide, cubeSide);
			        var gridSize = aABCGrid.GetSize();
			        for(short x = 0; x < gridSize.x; ++x) {
				        for(short y = 0; y < gridSize.y; ++y) {
					        for(short z = 0; z < gridSize.z; ++z) {
						        var cubeCenter = aABCGrid.GetAABCCenterFromGridCenter(x, y, z) + 
								        aABCGrid.GetCenter() + 
									        meshShellPositionFromObject;
						        if(aABCGrid.IsAABCSet(x, y, z)) {
							        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
							        Gizmos.DrawCube(cubeCenter, cubeSize);
						        } else if (drawEmptyCube) {
							        Gizmos.color = new Color(0f, 1f, 0f, 1f);	
							        Gizmos.DrawCube(cubeCenter, cubeSize);
						        }
					        }
				        }
			        }
		        }	
	        }
        }
    }
}
