using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace Voxelisation {
    public class Destruction : MonoBehaviour {

        private Vector3 hitPoint;
        private float hitForce;

        private Vector3 hit = new Vector3();

        private List<Vector3> voronoiPoints = new List<Vector3>();

        private Voxelisation.Voxelization.AABCGrid aabcGrid;

        public void setAabc(Voxelisation.Voxelization.AABCGrid grid) {
            this.aabcGrid = grid;
            findHitVoxel(hitPoint);
            generateVoronoiPoints(Mathf.Pow(hitForce, 1f/3f), 100);
        }

        public void setHitPoint(Vector3 hitPoint) {
            this.hitPoint = hitPoint;
        }

        public void setHitForce(float hitForce) {
            this.hitForce = hitForce;
        }

        public Vector3 getHit() { return hit; }

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

        private void generateVoronoiPoints(float radius, int numPoints) {
            for (int i= 0; i<numPoints; i++) {
                Vector3 voronoi = gaussianVector(radius);
                voronoiPoints.Add(toVoxelSpace(voronoi));
            }
        }

        public void Update() {
            if (voronoiPoints.Count > 0) {
                foreach (Vector3 vector in voronoiPoints) {
                    Debug.DrawLine(Vector3.zero, vector*aabcGrid.GetSize().side, Color.red);
                }
            }
        }
    }
}
