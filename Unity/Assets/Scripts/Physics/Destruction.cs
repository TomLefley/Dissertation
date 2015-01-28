using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace Voxelisation {
    public class Destruction : MonoBehaviour {

        private Vector3 hitPoint;
        private float hitForce;

        private PhysicalProperties physicalProperties;

        private Vector3 hit = new Vector3();

        private List<Vector3> voronoiPoints = new List<Vector3>();
        private List<Vector3> voronoiWorldPoints = new List<Vector3>();

        private List<MinMax> fragmentExtents = new List<MinMax>();
        private float[, ,] voronoiDiagram;

        private Voxelisation.Voxelization.AABCGrid aabcGrid;

        private List<Color> colors = new List<Color>();

        public void setAabc(Voxelisation.Voxelization.AABCGrid grid) {
            Clear();
            this.aabcGrid = grid;
            findHitVoxel(hitPoint);
            generateVoronoiPoints(calcRadius(hitForce), calcNumberOfPoints(hitForce));
            colourVoxels();
        }

        private void Clear() {
            voronoiPoints = new List<Vector3>();
            voronoiWorldPoints = new List<Vector3>();

            fragmentExtents = new List<MinMax>();

        }

        public void setHitPoint(Vector3 hitPoint) {
            this.hitPoint = hitPoint;
        }

        public void setHitForce(float hitForce) {
            this.hitForce = hitForce;
        }

        public void setPhysicalProperties(PhysicalProperties prop) {
            this.physicalProperties = prop;
        }

        public Vector3 getHit() { return hit; }
        public List<Vector3> getVoronoiPoints() { return voronoiPoints; }

        private void findHitVoxel(Vector3 hitPoint) {

            GridSize gridSize = aabcGrid.GetSize();
            Vector3 center = aabcGrid.GetCenter();

            Vector3 difference = hitPoint - center;

            short xOff, yOff, zOff;

            xOff = (short)(difference.x / gridSize.side);
            yOff = (short)(difference.y / gridSize.side);
            zOff = (short)(difference.z / gridSize.side);

            hit.x = (short)(gridSize.x / 2 + xOff);
            hit.y = (short)(gridSize.y / 2 + yOff);
            hit.z = (short)(gridSize.z / 2 + zOff);

        }

        private float gaussianRandom() {
            float u1 = Random.value;
            float u2 = Random.value;
            float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
                         Mathf.Sin(2.0f * Mathf.PI * u2); //random normal(0,1)
            float randNormal =
                         (1/3f) * randStdNormal; //random normal(mean,stdDev^2)

            if (Mathf.Abs(randNormal) > 1) randNormal /= Mathf.Abs(randNormal);

            return randNormal;
        }

        private Vector3 gaussianVector(float radius) {

            Vector3 v = Vector3.one;

            while (v.magnitude > 1.0f) {
                v = new Vector3(gaussianRandom(), gaussianRandom(), gaussianRandom());
            }

            return new Vector3(radius * v.x, radius * v.y, radius * v.z);

        }

        private Vector3 toVoxelSpace(Vector3 worldSpace) {
            float side = aabcGrid.GetSize().side;

            return new Vector3(worldSpace.x / side, worldSpace.y / side, worldSpace.z / side);
        }

        private Vector3 toVoxelGridSpace(Vector3 worldSpace) {
            Vector3 grid = new Vector3((int)(worldSpace.x + hit.x), (int)(worldSpace.y + hit.y), (int)(worldSpace.z + hit.z));
            if (grid.x < 0 || grid.y < 0 || grid.z < 0) {
                return Vector3.zero;
            }
            if (grid.x >= aabcGrid.GetSize().x || grid.y >= aabcGrid.GetSize().y || grid.z >= aabcGrid.GetSize().z) {
                return Vector3.zero;
            }
            if (aabcGrid.IsAABCSet((short)grid.x, (short)grid.y, (short)grid.z)) {
                return grid;
            }
            return Vector3.zero;
        }

        private void generateVoronoiPoints(float radius, int numPoints) {
            int i = 0;
            while (i<numPoints) {
                Vector3 voronoi = gaussianVector(radius);
                Vector3 voxelVoronoi = toVoxelGridSpace(toVoxelSpace(voronoi));
                if (voxelVoronoi != Vector3.zero) {
                    voronoiWorldPoints.Add(voronoi);
                    voronoiPoints.Add(voxelVoronoi);
                    colors.Add(new Color(Random.value, Random.value, Random.value, 0.5f));
                    i++;
                }
            }

            colors.Add(new Color(Random.value, Random.value, Random.value, 0.5f));
            colors.Add(new Color(Random.value, Random.value, Random.value, 0.5f));
        }

        private void colourVoxels() {
            if (voronoiPoints.Count == 0) {
                return;
            }

            var gridSize = aabcGrid.GetSize();
            voronoiDiagram = new float[gridSize.x, gridSize.y, gridSize.z];
            fragmentExtents.Add(null);

            for (int i = 0; i <= voronoiPoints.Count; i++) {
                fragmentExtents.Add(new MinMax());
            }

            Debug.Log(fragmentExtents.Count);

            for (short x = 0; x < gridSize.x; ++x) {
                for (short y = 0; y < gridSize.y; ++y) {
                    for (short z = 0; z < gridSize.z; ++z) {
                        Vector3 voxel = new Vector3(x, y, z);
                        if (aabcGrid.IsAABCSet(x, y, z)) {
                            int index = 0;
                            float magnitude = float.MaxValue;

                            for (short i = 0; i < voronoiPoints.Count; i++) {
                                Vector3 point = voronoiPoints[i];
                                float thisMagnitude = (point - voxel).magnitude;
                                if (thisMagnitude < magnitude) {
                                    magnitude = thisMagnitude;
                                    index = i+2;
                                }
                            }

                            float radiusMagnitude = ((1.5f*calcRadius(hitForce))/gridSize.side)-((voxel - hit).magnitude);
                            if (radiusMagnitude < magnitude) {
                                index = 1;
                            }

                            fragmentExtents[index].UpdateMinX(x);
                            fragmentExtents[index].UpdateMinY(y);
                            fragmentExtents[index].UpdateMinZ(z);

                            fragmentExtents[index].UpdateMaxX(x);
                            fragmentExtents[index].UpdateMaxY(y);
                            fragmentExtents[index].UpdateMaxZ(z);

                            fragmentExtents[index].colour = index;
                            fragmentExtents[index].number++;

                            voronoiDiagram[x, y, z] = index;

                        }
                    }
                }
            }

            March();
            if (fragmentExtents[1].number == 0) {
                GameObject.DestroyObject(gameObject);
            }

        }

        private void March() {
            for (int i = 1; i <= voronoiPoints.Count; i++) {
                gameObject.GetComponent<MarchingCubesDriver>().StartMarching(voronoiDiagram, fragmentExtents[i], aabcGrid);
            }
        }

        private float calcRadius(float hitForce) {
            //TODO physics
            return Mathf.Pow(hitForce, 1f / 3f);
        }

        private int calcNumberOfPoints(float hitForce) {
            //TODO physics
            return 10;
        }

        public void Update() {
            if (voronoiPoints.Count > 0) {
                foreach (Vector3 vector in voronoiWorldPoints) {
                    Debug.DrawLine(hitPoint, hitPoint+vector, Color.red);
                }
            }
        }

        void OnDrawGizmos() {
            Gizmos.color = new Color(1.0f, 0.0f, 0.0f, .5f);
            //DrawColouring();
        }

        void DrawColouring() {
            if (fragmentExtents.Count > 0) {
                var gridSize = aabcGrid.GetSize();
                var cubeSize = new Vector3(gridSize.side, gridSize.side, gridSize.side);

                for (short x = 0; x<gridSize.x; x++) {
                    for (short y = 0; y<gridSize.y; y++) {
                        for (short z = 0; z<gridSize.z; z++) {
                            int i = (int)voronoiDiagram[x, y, z];
                            if (i == 1) continue;
                            Gizmos.color = colors[i];
                            var cubeCenter = aabcGrid.GetAABCCenterFromGridCenter(x, y, z) +
                                    aabcGrid.GetCenter();
                            Gizmos.DrawCube(cubeCenter, cubeSize);

                        }
                    }
                }
                    
            }
        }

    }
}

public class MinMax {
    public short minX, minY, minZ = short.MaxValue;
    public short maxX, maxY, maxZ = short.MinValue;
    public int colour;
    public int number = 0;

    public void UpdateMinX(short x) {
        if (x < minX) minX = x;
    }

    public void UpdateMinY(short y) {
        if (y < minY) minY = y;
    }

    public void UpdateMinZ(short z) {
        if (z < minZ) minZ = z;
    }

    public void UpdateMaxX(short x) {
        if (x > maxX) maxX = x;
    }

    public void UpdateMaxY(short y) {
        if (y > maxY) maxY = y;
    }

    public void UpdateMaxZ(short z) {
        if (z > maxZ) maxZ = z;
    }
}
