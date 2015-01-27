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

        private List<List<Vector3>> voronoiDiagram = new List<List<Vector3>>();

        private Voxelisation.Voxelization.AABCGrid aabcGrid;

        private List<Color> colors = new List<Color>();

        public void setAabc(Voxelisation.Voxelization.AABCGrid grid) {
            this.aabcGrid = grid;
            findHitVoxel(hitPoint);
            generateVoronoiPoints(calcRadius(hitForce), calcNumberOfPoints(hitForce));
            colourVoxels();
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
            if (grid.x >= aabcGrid.GetSize().x || grid.y >= aabcGrid.GetSize().y || grid.z >= aabcGrid.GetSize().z) {
                return Vector3.zero;
            }
            if (aabcGrid.IsAABCSet((short)grid.x, (short)grid.y, (short)grid.z)) {
                return grid;
            }
            return Vector3.zero;
            //TODO outofrange
        }

        private void generateVoronoiPoints(float radius, int numPoints) {
            int i = 0;
            while (i<numPoints) {
                Vector3 voronoi = gaussianVector(radius);
                Vector3 voxelVoronoi = toVoxelGridSpace(toVoxelSpace(voronoi));
                if (voxelVoronoi != Vector3.zero) {
                    voronoiWorldPoints.Add(voronoi);
                    voronoiPoints.Add(voxelVoronoi);
                    i++;
                }
            }
        }

        private void colourVoxels() {
            if (voronoiPoints.Count == 0) {
                return;
            }

            for (int i = 0; i <= voronoiPoints.Count; i++) { //One more for outside radius
                voronoiDiagram.Add(new List<Vector3>());
            }

            var gridSize = aabcGrid.GetSize();
            for (short x = 0; x < gridSize.x; ++x) {
                for (short y = 0; y < gridSize.y; ++y) {
                    for (short z = 0; z < gridSize.z; ++z) {
                        Vector3 voxel = new Vector3(x, y, z);
                        if (aabcGrid.IsAABCSet(x, y, z)) {
                            int index = 0;
                            float magnitude = float.MaxValue;

                            for (int i = 0; i < voronoiPoints.Count; i++) {
                                Vector3 point = voronoiPoints[i];
                                float thisMagnitude = (point - voxel).magnitude;
                                if (thisMagnitude < magnitude) {
                                    magnitude = thisMagnitude;
                                    index = i+1;
                                }
                            }

                            float radiusMagnitude = ((1.5f*calcRadius(hitForce))/gridSize.side)-((voxel - hit).magnitude);
                            if (radiusMagnitude < magnitude) {
                                index = 0;
                            }

                            voronoiDiagram[index].Add(voxel);

                        }
                    }
                }
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
            DrawColouring();
        }

        void DrawColouring() {
            if (voronoiDiagram.Count > 0) {
                var gridSize = aabcGrid.GetSize();
                var cubeSize = new Vector3(gridSize.side, gridSize.side, gridSize.side);

                for (int i = 0; i < voronoiDiagram.Count; i++ ) {
                    if (i == 0) continue;
                    List<Vector3> list = voronoiDiagram[i];

                    if (colors.Count <= i) {
                        Color color = new Color(Random.value, Random.value, Random.value, 0.5f);
                        colors.Add(color);
                    }
                    Gizmos.color = colors[i];
                    foreach (Vector3 vector in list) {
                        var cubeCenter = aabcGrid.GetAABCCenterFromGridCenter((short)vector.x, (short)vector.y, (short)vector.z) +
                                aabcGrid.GetCenter();
                        Gizmos.DrawCube(cubeCenter, cubeSize);
                    }
                }
            }
        }

    }
}
