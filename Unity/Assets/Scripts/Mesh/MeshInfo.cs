using UnityEngine;
using System.Collections;

public class MeshInfo {


    public Vector3[] verts;
    public int[] index;

    public MeshInfo(Vector3[] verts, int[] index) {
        this.verts = verts;
        this.index = index;

    }
}
