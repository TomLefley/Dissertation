// Copyright (c) 2012 Vincenzo Giovanni Comito
// See the file license.txt for copying permission.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThreadSafeVoxelisation {
    public class ThreadSafeVoxelisationDriver {

        bool drawMeshInside;

        public bool includeChildren;
        public bool createMultipleGrids;
        public float cubeSide;
        private List<ThreadSafeVoxelisation.Voxelization.AABCGrid> aABCGrids;

        public bool debug;
        public ComputeShader shader;

        public ThreadSafeVoxelisationDriver(bool drawMeshInside, bool includeChildren, bool createMultipleGrids, float cubeSide, bool debug, ComputeShader shader) {
            this.drawMeshInside = drawMeshInside;
            this.includeChildren = includeChildren;
            this.createMultipleGrids = createMultipleGrids;
            this.cubeSide = cubeSide;
            this.debug = debug;
            this.shader = shader;

        }

        public void addGrid(List<ThreadSafeVoxelisation.Voxelization.AABCGrid> aABCGrids) {
            this.aABCGrids = aABCGrids;
        }

        public void addGrid(ThreadSafeVoxelisation.Voxelization.AABCGrid aABCGrid) {
            if (aABCGrids == null) {
                aABCGrids = new List<ThreadSafeVoxelisation.Voxelization.AABCGrid>();
            }
            this.aABCGrids.Add(aABCGrid);
        }

        public ThreadSafeVoxelisation.Voxelization.AABCGrid GetGrid() {
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

    }
}
