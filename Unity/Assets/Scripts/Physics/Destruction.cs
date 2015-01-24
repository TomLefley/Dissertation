using UnityEngine;
using System.Collections;


namespace Voxelisation {
    public class Destruction : MonoBehaviour {

        private Vector3 hitPoint;

        private short hitX, hitY, hitZ;

        private Voxelisation.Voxelization.AABCGrid aabcGrid;

        public void setAabc(Voxelisation.Voxelization.AABCGrid grid) {
            this.aabcGrid = grid;
            findHitVoxel();
        }

        public void setHitPoint(Vector3 hitPoint) {
            this.hitPoint = hitPoint;
        }

        public int getHitX() { return hitX; }
        public int getHitY() { return hitY; }
        public int getHitZ() { return hitZ; }

        private void findHitVoxel() {
            if (hitPoint == null) return;

            GridSize gridSize = aabcGrid.GetSize();
            Vector3 center = aabcGrid.GetCenter();

            Vector3 difference = hitPoint - center;

            short xOff, yOff, zOff;

            xOff = (short)(difference.x / gridSize.side);
            yOff = (short)(difference.y / gridSize.side);
            zOff = (short)(difference.z / gridSize.side);

            hitX = (short)(gridSize.x / 2 + xOff);
            hitY = (short)(gridSize.y / 2 + yOff);
            hitZ = (short)(gridSize.z / 2 + zOff);

            Debug.Log(hitX + "," + hitY + "," + hitZ);

        }
    }
}
