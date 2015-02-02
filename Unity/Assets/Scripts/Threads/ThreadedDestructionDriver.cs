using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThreadedDestructionDriver : MonoBehaviour {

    public bool drawMeshShell = true;
    public bool drawMeshInside = true;
    public bool drawEmptyCube = false;
    public bool includeChildren = true;
    public bool createMultipleGrids = true;
    public Vector3 meshShellPositionFromObject = Vector3.zero;
    public float cubeSide = 0.1f;

    public bool debug;
    public ComputeShader shader;

    ThreadedVoxelisation.ThreadedVoxelisationDriver voxelisationDriver;
    ThreadedDestruction destruction;
    ThreadedMarchingCubesDriver marchingDriver;

    ThreadedVoxelisation.Voxelization.AABCGrid grid;

    List<Colouring> fragments;
    float[, ,] colouring;

    public Material m_material;

    void Start() {
        voxelisationDriver = new ThreadedVoxelisation.ThreadedVoxelisationDriver();
        voxelisationDriver.drawMeshShell = drawMeshShell;
        voxelisationDriver.drawMeshInside = drawMeshInside;
        voxelisationDriver.drawEmptyCube = drawEmptyCube;
        voxelisationDriver.includeChildren = includeChildren;
        voxelisationDriver.createMultipleGrids = createMultipleGrids;
        voxelisationDriver.meshShellPositionFromObject = meshShellPositionFromObject;
        voxelisationDriver.cubeSide = cubeSide;
        voxelisationDriver.debug = debug;
        voxelisationDriver.shader = shader;

        destruction = new ThreadedDestruction();
        marchingDriver = new ThreadedMarchingCubesDriver();
    }


    public void ThreadedDestroy(Vector3 hitPoint, float hitForce, PhysicalProperties physicalProperties) {
        voxelisationDriver.StartVoxelise(gameObject);
        grid = voxelisationDriver.GetGrid();

        destruction.Fragment(grid, hitPoint, hitForce, physicalProperties);
        fragments = destruction.getFragmentExtents();
        colouring = destruction.getVoronoiDiagram();

        List<MeshInfo> meshinfos = new List<MeshInfo>();

        int i = 1;

        while(i < fragments.Count) {
            ThreadManager.RunAsync(() => {

                meshinfos.Add(new ThreadedMarchingCubesDriver().StartMarching(colouring, fragments[i++], grid));

            });
        }

        ThreadManager.RunAsync(() => {

            while (meshinfos.Count < fragments.Count - 1) {
                
            }

            ThreadManager.QueueOnMainThread(() => {

                for (int j = 0; j < meshinfos.Count; j++) {

                    Mesh mesh = new Mesh();
                    MeshInfo meshinfo = meshinfos[j];
                    Colouring coloured = meshinfo.colour;

                    mesh.vertices = meshinfo.verts;
                    mesh.triangles = meshinfo.index;

                    //The diffuse shader wants uvs so just fill with a empty array, there not actually used
                    mesh.uv = new Vector2[mesh.vertices.Length];
                    mesh.RecalculateNormals();

                    if (coloured.colour == 1) {
                        gameObject.GetComponent<MeshFilter>().mesh = mesh;
                        gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
                        transform.localScale = Vector3.one;
                    } else {
                        GameObject m_mesh = new GameObject("Fragment" + coloured.colour);
                        m_mesh.AddComponent<MeshFilter>();
                        m_mesh.AddComponent<MeshRenderer>();
                        m_mesh.AddComponent<MeshCollider>();
                        m_mesh.GetComponent<MeshCollider>().convex = true;
                        m_mesh.AddComponent<Rigidbody>();
                        m_mesh.rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                        m_mesh.renderer.material = m_material;
                        m_mesh.GetComponent<MeshCollider>().sharedMesh = mesh;
                        m_mesh.GetComponent<MeshFilter>().mesh = m_mesh.GetComponent<MeshCollider>().sharedMesh;
                    }
                }
            });
        });
    }

    public void Destroy(Vector3 hitPoint, float hitForce, PhysicalProperties physicalProperties) {
        voxelisationDriver.StartVoxelise(gameObject);
        grid = voxelisationDriver.GetGrid();

        destruction.Fragment(grid, hitPoint, hitForce, physicalProperties);
        fragments = destruction.getFragmentExtents();
        colouring = destruction.getVoronoiDiagram();

        foreach (Colouring colour in fragments) {
            if (colour == null) continue;

            MeshInfo meshinfo = marchingDriver.StartMarching(colouring, colour, grid);

            Mesh mesh = new Mesh();
            Colouring coloured = meshinfo.colour;

            mesh.vertices = meshinfo.verts;
            mesh.triangles = meshinfo.index;

            //The diffuse shader wants uvs so just fill with a empty array, there not actually used
            mesh.uv = new Vector2[mesh.vertices.Length];
            mesh.RecalculateNormals();

            if (coloured.colour == 0) continue;
            if (coloured.colour == 1) {
                gameObject.GetComponent<MeshFilter>().mesh = mesh;
                gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
                foreach (Transform trans in transform) {
                    Vector3 scale = trans.localScale;
                    scale.x *= transform.localScale.x;
                    scale.y *= transform.localScale.y;
                    scale.z *= transform.localScale.z;
                    trans.localScale = scale;
                }
                transform.localScale = Vector3.one;
            } else {
                GameObject m_mesh = new GameObject("Fragment" + coloured.colour);
                m_mesh.AddComponent<MeshFilter>();
                m_mesh.AddComponent<MeshRenderer>();
                m_mesh.AddComponent<MeshCollider>();
                m_mesh.GetComponent<MeshCollider>().convex = true;
                m_mesh.AddComponent<Rigidbody>();
                m_mesh.rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                m_mesh.renderer.material = m_material;
                m_mesh.GetComponent<MeshCollider>().sharedMesh = mesh;
                m_mesh.GetComponent<MeshFilter>().mesh = m_mesh.GetComponent<MeshCollider>().sharedMesh;
            }
        }
    }
}
