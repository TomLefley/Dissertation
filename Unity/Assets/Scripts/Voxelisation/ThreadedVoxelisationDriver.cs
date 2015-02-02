// Copyright (c) 2012 Vincenzo Giovanni Comito
// See the file license.txt for copying permission.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThreadedVoxelisation {
    public class ThreadedVoxelisationDriver {

        public bool drawMeshShell = true;
        public bool drawMeshInside = true;
        public bool drawEmptyCube = false;
        public bool includeChildren = true;
        public bool createMultipleGrids = true;
        public Vector3 meshShellPositionFromObject = Vector3.zero;
        public float cubeSide = 0.1f;
        private List<ThreadedVoxelisation.Voxelization.AABCGrid> aABCGrids;

        public bool debug;
        public ComputeShader shader;

        public void addGrid(List<ThreadedVoxelisation.Voxelization.AABCGrid> aABCGrids) {
            this.aABCGrids = aABCGrids;
        }

        public void addGrid(ThreadedVoxelisation.Voxelization.AABCGrid aABCGrid) {
            if (aABCGrids == null) {
                aABCGrids = new List<ThreadedVoxelisation.Voxelization.AABCGrid>();
            }
            this.aABCGrids.Add(aABCGrid);
        }

        public ThreadedVoxelisation.Voxelization.AABCGrid GetGrid() {
            return aABCGrids[0];
        }

        public void StartVoxelise(GameObject gameObject) {
            //	TestTriangleIntersectionAABC();
            if (createMultipleGrids && includeChildren) {
                addGrid(Voxelization.CreateMultipleGridsWithGameObjectMesh(gameObject, cubeSide, drawMeshInside, debug, shader));
            } else {
                addGrid(Voxelization.CreateGridWithGameObjectMesh(gameObject, cubeSide, includeChildren, drawMeshInside, debug, shader));
            }
        }

        void OnDrawGizmos() {
            Gizmos.color = new Color(1.0f, 0.0f, 0.0f, .5f);
            if (debug) {
                //DrawMeshShell();
            }
        }

        void DrawMeshShell() {
            if (aABCGrids != null) {
                foreach (Voxelization.AABCGrid aABCGrid in aABCGrids) {
                    if (drawMeshShell && (aABCGrid != null)) {
                        var cubeSize = new Vector3(cubeSide, cubeSide, cubeSide);
                        var gridSize = aABCGrid.GetSize();
                        for (short x = 0; x < gridSize.x; ++x) {
                            for (short y = 0; y < gridSize.y; ++y) {
                                for (short z = 0; z < gridSize.z; ++z) {
                                    var cubeCenter = aABCGrid.GetAABCCenterFromGridCenter(x, y, z) +
                                            aABCGrid.GetCenter() +
                                                meshShellPositionFromObject;
                                    if (aABCGrid.IsAABCSet(x, y, z)) {
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
}
