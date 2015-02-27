using UnityEngine;
using System.Collections;

public class MeshInfo {


    public Vector3[] verts;
    public int[] index;

    public Fragment colour;

    public MeshInfo(Vector3[] verts, int[] index, Fragment colour) {
        this.verts = verts;
        this.index = index;
        this.colour = colour;
    }
}
