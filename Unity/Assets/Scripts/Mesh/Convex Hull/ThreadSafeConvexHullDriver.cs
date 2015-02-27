using UnityEngine;
using System.Collections.Generic;
using MIConvexHull;

public class ThreadedConvexHullDriver {

    public enum MODE { CUBE_VOLUME, SPHERE_VOLUME, SPHERE_SURFACE };

    public MODE mode = MODE.CUBE_VOLUME;
    public int NumberOfVertices = 1000;
    public double size = 5;

    Material lineMaterial;
    Mesh mesh;

    List<Vertex3> convexHullVertices;
    List<Face3> convexHullFaces;
    List<int> convexHullIndices;
    Matrix4x4 rotation = Matrix4x4.identity;

    float theta;
    bool drawHull = true;

    // Use this for initialization
    public MeshInfo StartMeshing(Colouring colour) {

        ConvexHull<Vertex3, Face3> convexHull = ConvexHull.Create<Vertex3, Face3>(colour.vertices);

        convexHullVertices = new List<Vertex3>(convexHull.Points);
        Vector3[] convexHullVectors = new Vector3[convexHullVertices.Count];
        convexHullFaces = new List<Face3>(convexHull.Faces);
        convexHullIndices = new List<int>();

        foreach (Face3 f in convexHullFaces) {
            convexHullIndices.Add(convexHullVertices.IndexOf(f.Vertices[0]));
            convexHullIndices.Add(convexHullVertices.IndexOf(f.Vertices[1]));
            convexHullIndices.Add(convexHullVertices.IndexOf(f.Vertices[2]));
        }

        for (int i = 0; i<convexHullVertices.Count; i++) {
            convexHullVectors[i] = convexHullVertices[i].ToVector3();
        }

        return new MeshInfo(convexHullVectors, convexHullIndices.ToArray(), colour);

    }
}



















