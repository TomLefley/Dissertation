using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Fragment {
    public short minX, minY, minZ = short.MaxValue;
    public short maxX, maxY, maxZ = short.MinValue;

    public short colour;

    //public int number = 0;
    //public bool main = false;

    public List<Vector3> vertices = new List<Vector3>();

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

    public Fragment(short colour) {
        this.colour = colour;
    }
}
