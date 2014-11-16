// Copyright (c) 2012 Vincenzo Giovanni Comito
// See the file license.txt for copying permission.

// Voxelization.js
// Written by Clynamen, 23/08/2012
// Utility for voxelization of a mesh.
// Thanks to Mike Vandelay for the 
// AABB-Triangle SAT implementation in C++.

//Translated to C# by Tom Lefley 16/11/2014

using System.Collections.Generic;
using UnityEngine;

namespace Voxelisation {
    public class GridSize {

        public int x;
        public int y;
        public int z;
        public float side;

        public GridSize(GridSize gridSize) {
            this.x = gridSize.x;
            this.y = gridSize.y;
            this.z = gridSize.z;
            this.side = gridSize.side;
        }

        public GridSize(int x, int y, int z, float side) {
            this.x = x;
            this.y = y;
            this.z = z;
            this.side = side;
        }

    };

    public class ThreadedJob {
        private bool m_IsDone = false;
        private object m_Handle = new object();
        private System.Threading.Thread m_Thread = null;
        public bool IsDone {
            get {
                bool tmp;
                lock (m_Handle) {
                    tmp = m_IsDone;
                }
                return tmp;
            }
            set {
                lock (m_Handle) {
                    m_IsDone = value;
                }
            }
        }

        public virtual void Start() {
            m_Thread = new System.Threading.Thread(Run);
            m_Thread.Start();
        }
        public virtual void Abort() {
            m_Thread.Abort();
        }

        protected virtual void ThreadFunction() { }

        protected virtual void OnFinished() { }

        public virtual bool Update() {
            if (IsDone) {
                OnFinished();
                return true;
            }
            return false;
        }
        private void Run() {
            ThreadFunction();
            IsDone = true;
        }
    };

    public class Job : ThreadedJob {

        public bool[, ,] cubeSet;
        public short[, ,] cubeNormalSum;

        public Voxelisation.Voxelization.AABCGrid grid;
        public Vector3[] triangle;
        public Vector3 origin;
        public float side;
        public bool storeNormalSum;
        public float ignoreNormalRange;

        short x, y, z;

        protected override void ThreadFunction() {
            // Find the triangle AABB, select a sub grid.
            short startX = (short)Mathf.FloorToInt((Mathf.Min(triangle[0].x, triangle[1].x, triangle[2].x) - origin.x) / side);
            short startY = (short)Mathf.FloorToInt((Mathf.Min(triangle[0].y, triangle[1].y, triangle[2].y) - origin.y) / side);
            short startZ = (short)Mathf.FloorToInt((Mathf.Min(triangle[0].z, triangle[1].z, triangle[2].z) - origin.z) / side);
            short endX = (short)Mathf.CeilToInt((Mathf.Max(triangle[0].x, triangle[1].x, triangle[2].x) - origin.x) / side);
            short endY = (short)Mathf.CeilToInt((Mathf.Max(triangle[0].y, triangle[1].y, triangle[2].y) - origin.y) / side);
            short endZ = (short)Mathf.CeilToInt((Mathf.Max(triangle[0].z, triangle[1].z, triangle[2].z) - origin.z) / side);
            if (storeNormalSum) {
                for (x = startX; x <= endX; ++x) {
                    for (y = startY; y <= endY; ++y) {
                        for (z = startZ; z <= endZ; ++z) {
                            if (grid.TriangleIntersectAABC(triangle, x, y, z)) {
                                var triangleNormal = Voxelization.GetTriangleNormal(triangle);
                                cubeSet[x, y, z] = true;
                                if (triangleNormal.z < 0 - ignoreNormalRange) {
                                    cubeNormalSum[x, y, z]++;
                                } else if (triangleNormal.z > 0 + ignoreNormalRange) {
                                    cubeNormalSum[x, y, z]--;
                                }
                            }
                        }
                    }
                }
            } else {
                for (x = startX; x < endX; ++x) {
                    for (y = startY; y < endY; ++y) {
                        for (z = startZ; z < endZ; ++z) {
                            if (!grid.IsAABCSet(x, y, z) && grid.TriangleIntersectAABC(triangle, x, y, z)) {
                                cubeSet[x, y, z] = true;
                            }
                        }
                    }
                }
            }
        }
        protected override void OnFinished() {
            // This is executed by the Unity main thread when the job is finished
            
        }
    };

    static public class Voxelization {

        public class AABCGrid {

            private float side;
            private short width;
            private short height;
            private short depth;
            private Vector3 origin;
            private bool[, ,] cubeSet;
            private short[, ,] cubeNormalSum;
            private bool debug = true;

            protected class AABCPosition {

                public short x;
                public short y;
                public short z;

                public AABCPosition(AABCPosition aABCPosition) {
                    this.x = aABCPosition.x;
                    this.y = aABCPosition.y;
                    this.z = aABCPosition.z;
                }

                public AABCPosition(short x, short y, short z) {
                    this.x = x;
                    this.y = y;
                    this.z = z;
                }

            };

            protected class AABC : AABCPosition {

                private AABCGrid grid;

                public AABC(AABC aABC)
                    : base(aABC) {
                    this.grid = aABC.grid;
                }

                public AABC(short x, short y, short z, AABCGrid grid)
                    : base(x, y, z) {
                    this.grid = grid;
                }

                public AABC(AABCPosition position, AABCGrid grid)
                    : base(position) {
                    this.grid = grid;
                }

                public Vector3 GetCenter() {
                    return grid.GetAABCCenter(x, y, z);
                }

                public AABCGrid GetGrid() {
                    return grid;
                }

                public bool IsSet() {
                    return grid.IsAABCSet(x, y, z);
                }

                public Vector3[] GetCorners(short x, short y, short z) {
                    return grid.GetAABCCorners(x, y, z);
                }

            };

            protected abstract class AABCGridIteratorBase {

                protected AABCGrid grid;
                protected AABCPosition position;

                protected AABCGridIteratorBase(AABCGrid grid) {
                    this.grid = grid;
                }

                public abstract bool HasNext();

                public abstract AABC Next();

            };

            protected class AABCGridIterator : AABCGridIteratorBase {

                public AABCGridIterator(AABCGrid grid)
                    : base(grid) {
                }

                public override bool HasNext() {
                    if (position.x == grid.width &&
                        position.y == grid.height &&
                        position.z == grid.depth) {
                        return false;
                    }
                    return true;
                }

                public override AABC Next() {
                    position.z++;
                    if (position.z == grid.depth) {
                        position.z = 0;
                        position.y++;
                        if (position.y == grid.height) {
                            position.y = 0;
                            position.x++;
                            if (position.x > grid.width) {
                                throw new System.IndexOutOfRangeException();
                            }
                        }
                    }
                    return new AABC(position, grid);
                }

            }

            protected class AABCGridSetAABCIterator : AABCGridIteratorBase {
                private bool nextFound;
                private AABCPosition nextPosition;

                public AABCGridSetAABCIterator(AABCGrid grid)
                    : base(grid) {
                    position = new AABCPosition(0, 0, 0);
                    if (grid.IsAABCSet(position)) {
                        nextPosition = position;
                    }
                    nextFound = true;
                }

                public override bool HasNext() {
                    if (!nextFound) {
                        return FindNext();
                    }
                    return true;
                }

                public override AABC Next() {
                    if (!nextFound) {
                        FindNext();
                    }
                    position = nextPosition;
                    nextFound = false;
                    return new AABC(position, grid);
                }

                private bool FindNext() {
                    nextPosition = new AABCPosition(position);
                    nextPosition.z++;
                    for (; nextPosition.x < grid.width; nextPosition.x++) {
                        for (; nextPosition.y < grid.height; nextPosition.y++) {
                            for (; nextPosition.z < grid.depth; nextPosition.z++) {
                                if (grid.IsAABCSet(nextPosition.x, nextPosition.y, nextPosition.z)) {
                                    nextFound = true;
                                    return true;
                                }
                            }
                            nextPosition.z = 0;
                        }
                        nextPosition.y = 0;
                    }
                    nextFound = false;
                    return false;
                }
            }

            public AABCGrid(short x, short y, short z, float sideLength) {
                width = x;
                height = y;
                depth = z;
                side = sideLength;
                origin = new Vector3();
                cubeSet = new bool[width, height, depth];
            }

            public AABCGrid(short x, short y, short z, float sideLength, Vector3 ori) {
                width = x;
                height = y;
                depth = z;

                side = sideLength;
                origin = ori;
                cubeSet = new bool[width, height, depth];
            }

            public void CleanGrid() {
                cubeSet = new bool[width, height, depth];
            }

            public void SetDebug(bool debug) {
                this.debug = debug;
            }

            public GridSize GetSize() {
                return new GridSize(width, height, depth, side);
            }

            public void SetSize(GridSize dimension) {
                SetSize((short)dimension.x, (short)dimension.y, (short)dimension.z, dimension.side);
            }

            public void SetSize(short x, short y, short z, float sideLength) {
                width = x;
                height = y;
                depth = z;
                side = sideLength;
                CleanGrid();
            }

            public Vector3 GetCenter() {
                return origin + new Vector3(width / 2 * side, height / 2 * side, depth / 2 * side);
            }

            public void SetCenter(Vector3 pos) {
                origin = pos - new Vector3(width / 2 * side, height / 2 * side, depth / 2 * side);
            }

            public int GetSetAABCCount() {
                int count = 0;
                for (short x = 0; x < width; ++x) {
                    for (short y = 0; y < height; ++y) {
                        for (short z = 0; z < depth; ++z) {
                            if (!IsAABCSet(x, y, z)) {
                                count++;
                            }
                        }
                    }
                }
                return count;
            }

            protected Vector3[] GetAABCCorners(AABCPosition pos) {
                CheckBounds(pos.x, pos.y, pos.z);
                return GetAABCCornersUnchecked(pos.x, pos.y, pos.z);
            }

            public Vector3[] GetAABCCorners(short x, short y, short z) {
                return GetAABCCornersUnchecked(x, y, z);
            }

            protected Vector3[] GetAABCCornersUnchecked(short x, short y, short z) {
                var center = new Vector3(x * side + side / 2, y * side + side / 2, z * side + side / 2);
                var corners = new Vector3[8];
                corners[0] = new Vector3(center.x + side, center.y - side, center.z + side) + origin;
                corners[1] = new Vector3(center.x + side, center.y - side, center.z - side) + origin;
                corners[2] = new Vector3(center.x - side, center.y - side, center.z - side) + origin;
                corners[3] = new Vector3(center.x - side, center.y - side, center.z + side) + origin;
                corners[4] = new Vector3(center.x + side, center.y + side, center.z + side) + origin;
                corners[5] = new Vector3(center.x + side, center.y + side, center.z - side) + origin;
                corners[6] = new Vector3(center.x - side, center.y + side, center.z - side) + origin;
                corners[7] = new Vector3(center.x - side, center.y + side, center.z + side) + origin;
                return corners;
            }

            protected Vector3 GetAABCCenter(AABCPosition pos) {
                CheckBounds(pos.x, pos.y, pos.z);
                return GetAABCCenterUnchecked(pos.x, pos.y, pos.z);
            }

            public Vector3 GetAABCCenter(short x, short y, short z) {
                CheckBounds(x, y, z);
                return GetAABCCenterUnchecked(x, y, z);
            }

            protected Vector3 GetAABCCenterUnchecked(short x, short y, short z) {
                return GetAABCCenterFromGridCenterUnchecked(x, y, z) + GetCenter();
            }

            protected Vector3 GetAABCCenterFromGridOrigin(AABCPosition pos) {
                CheckBounds(pos.x, pos.y, pos.z);
                return GetAABCCenterFromGridOriginUnchecked(pos.x, pos.y, pos.z);
            }

            public Vector3 GetAABCCenterFromGridOrigin(short x, short y, short z) {
                CheckBounds(x, y, z);
                return GetAABCCenterFromGridOriginUnchecked(x, y, z);
            }

            protected Vector3 GetAABCCenterFromGridOriginUnchecked(short x, short y, short z) {
                return new Vector3(x * side + side / 2, y * side + side / 2, z * side + side / 2);
            }

            protected Vector3 GetAABCCenterFromGridCenter(AABCPosition pos) {
                CheckBounds(pos.x, pos.y, pos.z);
                return GetAABCCenterFromGridCenterUnchecked(pos.x, pos.y, pos.z);
            }

            public Vector3 GetAABCCenterFromGridCenter(short x, short y, short z) {
                CheckBounds(x, y, z);
                return GetAABCCenterFromGridCenterUnchecked(x, y, z);
            }

            protected Vector3 GetAABCCenterFromGridCenterUnchecked(short x, short y, short z) {
                return new Vector3(side * (x + 1 / 2 - width / 2),
                                side * (y + 1 / 2 - height / 2),
                                side * (z + 1 / 2 - depth / 2));
            }

            protected bool IsAABCSet(AABCPosition pos) {
                CheckBounds(pos.x, pos.y, pos.z);
                return IsAABCSetUnchecked(pos.x, pos.y, pos.z);
            }

            public bool IsAABCSet(short x, short y, short z) {
                CheckBounds(x, y, z);
                return IsAABCSetUnchecked(x, y, z);
            }

            protected bool IsAABCSetUnchecked(short x, short y, short z) {
                return cubeSet[x, y, z];
            }

            protected bool TriangleIntersectAABC(Vector3[] triangle, AABCPosition pos) {
                CheckBounds(pos.x, pos.y, pos.z);
                return TriangleIntersectAABCUnchecked(triangle, pos.x, pos.y, pos.z);
            }

            public bool TriangleIntersectAABC(Vector3[] triangle, short x, short y, short z) {
                CheckBounds(x, y, z);
                return TriangleIntersectAABCUnchecked(triangle, x, y, z);
            }

            protected bool TriangleIntersectAABCUnchecked(Vector3[] triangle, short x, short y, short z) {
                Vector3[] aabcCorners;
                Vector3 triangleEdgeA;
                Vector3 triangleEdgeB;
                Vector3 triangleEdgeC;
                Vector3 triangleNormal;
                Vector3 aabcEdgeA = new Vector3(1, 0, 0);
                Vector3 aabcEdgeB = new Vector3(0, 1, 0);
                Vector3 aabcEdgeC = new Vector3(0, 0, 1);

                aabcCorners = GetAABCCornersUnchecked(x, y, z);

                triangleEdgeA = triangle[1] - triangle[0];
                triangleEdgeB = triangle[2] - triangle[1];
                triangleEdgeC = triangle[0] - triangle[2];

                if (!ProjectionsIntersectOnAxis(aabcCorners, triangle, Vector3.Cross(triangleEdgeA, aabcEdgeA))) return false;
                if (!ProjectionsIntersectOnAxis(aabcCorners, triangle, Vector3.Cross(triangleEdgeA, aabcEdgeB))) return false;
                if (!ProjectionsIntersectOnAxis(aabcCorners, triangle, Vector3.Cross(triangleEdgeA, aabcEdgeC))) return false;
                if (!ProjectionsIntersectOnAxis(aabcCorners, triangle, Vector3.Cross(triangleEdgeB, aabcEdgeA))) return false;
                if (!ProjectionsIntersectOnAxis(aabcCorners, triangle, Vector3.Cross(triangleEdgeB, aabcEdgeB))) return false;
                if (!ProjectionsIntersectOnAxis(aabcCorners, triangle, Vector3.Cross(triangleEdgeB, aabcEdgeC))) return false;
                if (!ProjectionsIntersectOnAxis(aabcCorners, triangle, Vector3.Cross(triangleEdgeC, aabcEdgeA))) return false;
                if (!ProjectionsIntersectOnAxis(aabcCorners, triangle, Vector3.Cross(triangleEdgeC, aabcEdgeB))) return false;
                if (!ProjectionsIntersectOnAxis(aabcCorners, triangle, Vector3.Cross(triangleEdgeC, aabcEdgeC))) return false;

                triangleNormal = Vector3.Cross(triangleEdgeA, triangleEdgeB);
                if (!ProjectionsIntersectOnAxis(aabcCorners, triangle, triangleNormal)) return false;

                if (!ProjectionsIntersectOnAxis(aabcCorners, triangle, aabcEdgeA)) return false;
                if (!ProjectionsIntersectOnAxis(aabcCorners, triangle, aabcEdgeB)) return false;
                if (!ProjectionsIntersectOnAxis(aabcCorners, triangle, aabcEdgeC)) return false;

                return true;
            }

            protected void CheckBounds(short x, short y, short z) {
                if (x < 0 || y < 0 || z < 0 || x >= width || y >= height || z >= depth) {
                    throw new System.ArgumentOutOfRangeException("The requested AABC is out of the grid limits.");
                }
            }

            public void FillGridWithGameObjectMeshShell(GameObject gameObj) {
                FillGridWithGameObjectMeshShell(gameObj, false);
            }

            public void FillGridWithGameObjectMeshShell(GameObject gameObj, bool storeNormalSum) {
                List<Job> jobs = new List<Job>();

                Mesh gameObjMesh = gameObj.GetComponent<MeshFilter>().mesh;
                Transform gameObjTransf = gameObj.transform;
                float startTime = Time.realtimeSinceStartup;
                Vector3[] meshVertices = gameObjMesh.vertices;
                int[] meshTriangles = gameObjMesh.triangles;
                int meshTrianglesCount = meshTriangles.Length / 3;

                // In this method we can also evaluate stores the normals of the triangles 
                // that intersect the cube.
                if (storeNormalSum) {
                    cubeNormalSum = new short[width, height, depth];
                }
                if (debug) {
                    Debug.Log("Start:");
                    Debug.Log("Time: " + startTime);
                    Debug.Log("		Mesh Description: ");
                    Debug.Log("Name: " + gameObjMesh.name);
                    Debug.Log("Triangles: " + meshTrianglesCount);
                    Debug.Log("Local AABB size: " + gameObjMesh.bounds.size);
                    Debug.Log("		AABCGrid Description:");
                    Debug.Log("Size: " + width + ',' + height + ',' + depth);
                }

                // For each triangle, perform SAT intersection check with the AABCs within the triangle AABB.
                for (int i = 0; i < meshTrianglesCount; ++i) {
                    Vector3[] triangle = new Vector3[3];
                    triangle[0] = gameObjTransf.TransformPoint(meshVertices[meshTriangles[i * 3]]);
                    triangle[1] = gameObjTransf.TransformPoint(meshVertices[meshTriangles[i * 3 + 1]]);
                    triangle[2] = gameObjTransf.TransformPoint(meshVertices[meshTriangles[i * 3 + 2]]);

                    Job myJob = new Job();
                    myJob.grid = this;
                    myJob.side = side;
                    myJob.ignoreNormalRange = 0;
                    myJob.origin = origin;
                    myJob.storeNormalSum = storeNormalSum;
                    myJob.triangle = triangle;

                    jobs.Add(myJob);

                    myJob.Start();

                }

                bool finished = false;

                /*while (!finished) {
                    finished = true;
                    foreach (Job job in jobs) {
                        if (job != null) {
                            if (!job.Update()) {
                                finished = false;
                            }
                        }
                    }
                }*/

                if (debug) {
                    Debug.Log("Grid Evaluation Ended!");
                    Debug.Log("Time spent: " + (Time.realtimeSinceStartup - startTime) + "s");
                    Debug.Log("End: ");
                }
            }

            public void FillGridWithGameObjectMesh(GameObject gameObj) {
                FillGridWithGameObjectMeshShell(gameObj, true);

                for (var x = 0; x < width; ++x) {
                    for (var y = 0; y < height; ++y) {
                        var fill = false;
                        var cubeToFill = 0;
                        for (var z = 0; z < depth; ++z) {
                            if (cubeSet[x, y, z]) {
                                var normalSum = cubeNormalSum[x, y, z];
                                if (normalSum != 0) {
                                    if (normalSum > 0) {
                                        fill = true;
                                    } else {
                                        fill = false;
                                        while (cubeToFill > 1) {
                                            cubeToFill--;
                                            cubeSet[x, y, z - cubeToFill] = true;
                                        }
                                    }
                                    cubeToFill = 0;
                                }
                                continue;
                            }
                            if (fill) {
                                cubeToFill++;
                            }
                        }
                    }
                }
                cubeNormalSum = null;
            }

            public ParticleSystem.Particle[] AddParticles(ParticleSystem.Particle[] particles, int particlesToAdd) {
                var settedAABCCount = GetSetAABCCount();
                int particlesPerAABC;
                var randMax = side / 2;
                var addedParticles = 0;
                AABC cube;
                var i = 0;

                if (particlesToAdd <= 0) {
                    throw new System.ArgumentException("The number of particles to add is < 0");
                }

                particlesPerAABC = particlesToAdd / settedAABCCount;
                if (particlesPerAABC <= 0) {
                    particlesPerAABC = 1;
                }

                while (particlesToAdd > 0) {
                    var iter = new AABCGridSetAABCIterator(this);
                    var cubeFilledCount = 0;
                    while (iter.HasNext()) {
                        cube = iter.Next();
                        for (; i < addedParticles + particlesPerAABC && particlesToAdd > 0; ++i) {
                            particles[i].position = cube.GetCenter() +
                                            new Vector3(Random.Range(-randMax, randMax),
                                                        Random.Range(-randMax, randMax),
                                                        Random.Range(-randMax, randMax)) / 100;
                            particlesToAdd--;
                        }
                        addedParticles += particlesPerAABC;
                        cubeFilledCount++;
                        if (particlesToAdd <= 0) {
                            break;
                        }
                    }
                    if (particlesToAdd > 0) {
                        particlesPerAABC = 1;
                    }
                }

                return particles;
            }

        };

        public static bool ProjectionsIntersectOnAxis(Vector3[] solidA, Vector3[] solidB, Vector3 axis) {
            var minSolidA = MinOfProjectionOnAxis(solidA, axis);
            var maxSolidA = MaxOfProjectionOnAxis(solidA, axis);
            var minSolidB = MinOfProjectionOnAxis(solidB, axis);
            var maxSolidB = MaxOfProjectionOnAxis(solidB, axis);

            if (minSolidA > maxSolidB) {
                return false;
            }
            if (maxSolidA < minSolidB) {
                return false;
            }

            return true;
        }

        public static float MinOfProjectionOnAxis(Vector3[] solid, Vector3 axis) {
            var min = Mathf.Infinity;
            float dotProd;

            for (var i = 0; i < solid.Length; ++i) {
                dotProd = Vector3.Dot(solid[i], axis);
                if (dotProd < min) {
                    min = dotProd;
                }
            }
            return min;
        }

        public static float MaxOfProjectionOnAxis(Vector3[] solid, Vector3 axis) {
            var max = Mathf.NegativeInfinity;
            float dotProd;

            for (var i = 0; i < solid.Length; ++i) {
                dotProd = Vector3.Dot(solid[i], axis);
                if (dotProd > max) {
                    max = dotProd;
                }
            }
            return max;
        }

        // Return a vector with the minimum components
        public static Vector3 MinVectorComponents(Vector3 a, Vector3 b) {
            var ret = new Vector3();
            ret.x = Mathf.Min(a.x, b.x);
            ret.y = Mathf.Min(a.y, b.y);
            ret.z = Mathf.Min(a.z, b.z);
            return ret;
        }

        // Return a vector with the minimum components
        public static Vector3 MaxVectorComponents(Vector3 a, Vector3 b) {
            var ret = new Vector3();
            ret.x = Mathf.Max(a.x, b.x);
            ret.y = Mathf.Max(a.y, b.y);
            ret.z = Mathf.Max(a.z, b.z);
            return ret;
        }

        public static Vector3 GetTriangleNormal(Vector3[] triangle) {
            return Vector3.Cross(triangle[1] - triangle[0], triangle[2] - triangle[0]).normalized;
        }

        // Return an AABB which include the meshes of the object itself and of its children
        public static Bounds GetTotalBoundsOfGameObject(GameObject gameObj) {
            var totalBounds = new Bounds();
            Vector3 min = new Vector3();
            Vector3 max = new Vector3();
            if (gameObj.renderer) {
                min = gameObj.renderer.bounds.min;
                max = gameObj.renderer.bounds.max;
            }

            for (var i = 0; i < gameObj.transform.childCount; ++i) {
                var childObj = gameObj.transform.GetChild(i).gameObject;
                var childTotalBounds = GetTotalBoundsOfGameObject(childObj);
                min = MinVectorComponents(min, childTotalBounds.min);
                max = MaxVectorComponents(max, childTotalBounds.max);
            }

            totalBounds.SetMinMax(min, max);
            return totalBounds;
        }

        public static List<GameObject> GetChildrenWithMesh(GameObject gameObj) {
            var ret = new List<GameObject>();
            for (var i = 0; i < gameObj.transform.childCount; ++i) {
                var childObj = gameObj.transform.GetChild(i).gameObject;
                if (childObj.renderer) {
                    ret.Add(childObj);
                }
                ret.AddRange(GetChildrenWithMesh(childObj));
            }
            return ret;
        }

        // Warning: this method creates a grid at least as big as the total bounding box of the
        // game object, if children are included there may be empty space. Consider to use 
        // CreateMultipleGridsWithGameObjectMeshShell in order to save memory.
        public static AABCGrid CreateGridWithGameObjectMesh(GameObject gameObj,
                                        float cubeSide, bool includeChildren, bool includeInside) {
            AABCGrid aABCGrid;
            short width;
            short height;
            short depth;
            var origin = new Vector3();
            List<GameObject> gameObjectsWithMesh;
            var gridBoundsMin = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            var gridBoundsMax = new Vector3(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);

            if (includeChildren) {
                gameObjectsWithMesh = GetChildrenWithMesh(gameObj);
            } else {
                gameObjectsWithMesh = new List<GameObject>();
            }
            if (gameObj.renderer) {
                gameObjectsWithMesh.Add(gameObj);
            }


            foreach (GameObject gameObjectWithMesh in gameObjectsWithMesh) {
                gridBoundsMin = MinVectorComponents(gridBoundsMin,
                                            gameObjectWithMesh.renderer.bounds.min);
                gridBoundsMax = MaxVectorComponents(gridBoundsMax,
                                            gameObjectWithMesh.renderer.bounds.max);
            }
            width = (short)(Mathf.Ceil((gridBoundsMax.x - gridBoundsMin.x) / cubeSide) + 2);
            height = (short)(Mathf.Ceil((gridBoundsMax.y - gridBoundsMin.y) / cubeSide) + 2);
            depth = (short)(Mathf.Ceil((gridBoundsMax.z - gridBoundsMin.z) / cubeSide) + 2);
            origin = gridBoundsMin - new Vector3(cubeSide, cubeSide, cubeSide);
            aABCGrid = new AABCGrid(width, height, depth, cubeSide, origin);
            foreach (GameObject gameObjectWithMesh in gameObjectsWithMesh) {
                if (includeInside) {
                    aABCGrid.FillGridWithGameObjectMesh(gameObjectWithMesh);
                } else {
                    aABCGrid.FillGridWithGameObjectMeshShell(gameObjectWithMesh);
                }
            }

            return aABCGrid;
        }

        public static List<AABCGrid> CreateMultipleGridsWithGameObjectMesh(GameObject gameObj,
                                        float cubeSide, bool includeMeshInside) {
            List<GameObject> gameObjectsWithMesh;
            var grids = new List<AABCGrid>();

            gameObjectsWithMesh = GetChildrenWithMesh(gameObj);
            if (gameObj.renderer) {
                gameObjectsWithMesh.Add(gameObj);
            }

            foreach (GameObject gameObjWithMesh in gameObjectsWithMesh) {
                grids.Add(CreateGridWithGameObjectMesh(gameObjWithMesh, cubeSide, false, includeMeshInside));
            }

            return grids;
        }

    };
}
