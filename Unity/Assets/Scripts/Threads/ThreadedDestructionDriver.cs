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

    public List<string> messages = new List<string>();

    ThreadedVoxelisation.ThreadedVoxelisationDriver voxelisationDriver;
    ThreadedDestruction destruction;
    ThreadedMarchingCubesDriver marchingDriver;
    ThreadedConvexHullDriver convexDriver;
    ConstructiveSolidGeometry csg;

    ThreadedVoxelisation.Voxelization.AABCGrid grid;

    Dictionary<short, Colouring> fragments;
    short[, ,] colouring;

    public Material m_material;

    Dictionary<short, Color> colors = new Dictionary<short, Color>();

    bool done = false;

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
        convexDriver = new ThreadedConvexHullDriver();
        csg = new ConstructiveSolidGeometry();
    }


    public void ThreadedDestroy(Vector3 hitPoint, float hitForce, PhysicalProperties physicalProperties) {
        voxelisationDriver.StartVoxelise(gameObject);
        grid = voxelisationDriver.GetGrid();

        destruction.Fragment(grid, hitPoint, hitForce, physicalProperties);
        fragments = destruction.getFragmentExtents();
        colouring = destruction.getVoronoiDiagram();

        List<MeshInfo> meshinfos = new List<MeshInfo>();

        short i = 1;

        while(i < fragments.Count) {
            ThreadManager.RunAsync(() => {

                Colouring itColouring;
                bool found = fragments.TryGetValue(i, out itColouring);

                if (found) {
                    meshinfos.Add(new ThreadedMarchingCubesDriver().StartMarching(colouring, itColouring, grid));
                }

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

        messages.Add("Starting");
        float time = Time.realtimeSinceStartup;
        float startTime = time;

        voxelisationDriver.StartVoxelise(gameObject);

        messages.Add("Voxelisation: " + (Time.realtimeSinceStartup - time));
        time = Time.realtimeSinceStartup;

        grid = voxelisationDriver.GetGrid();

        destruction.Fragment(grid, hitPoint, hitForce, physicalProperties);

        messages.Add("Destruction: " + (Time.realtimeSinceStartup - time));
        time = Time.realtimeSinceStartup;

        fragments = destruction.getFragmentExtents();
        colouring = destruction.getVoronoiDiagram();

        bool main = false;

        Colouring allFrags = new Colouring(999);

        foreach (Colouring colour in fragments.Values) {
            if (colour.main) continue;
            allFrags.UpdateMinX(colour.minX);
            allFrags.UpdateMinY(colour.minY);
            allFrags.UpdateMinZ(colour.minZ);
            allFrags.UpdateMaxX(colour.maxX);
            allFrags.UpdateMaxY(colour.maxY);
            allFrags.UpdateMaxZ(colour.maxZ);
            allFrags.vertices.AddRange(colour.vertices);
        }

        foreach (Colouring colour in fragments.Values) {
            if (colour == null) continue;

            MeshInfo meshinfo;

            colors.Add(colour.colour, new Color(Random.value, Random.value, Random.value));

            if (colour.main) {
                //meshinfo = marchingDriver.StartMarching(colouring, colour, grid);
                meshinfo = csg.StartMeshing(new MeshInfo(gameObject.GetComponent<MeshFilter>().mesh.vertices, gameObject.GetComponent<MeshFilter>().mesh.triangles, colour), colour, new List<Colouring>(fragments.Values), colouring, grid.GetSize());
            } else {
                //meshinfo = convexDriver.StartMeshing(colour);
                meshinfo = marchingDriver.StartMarching(colouring, colour, grid);
            }

            messages.Add("Meshing " + colour.colour + " " + colour.main + ": " + (Time.realtimeSinceStartup - time));
            time = Time.realtimeSinceStartup;

            Mesh mesh = new Mesh();
            Colouring coloured = meshinfo.colour;

            mesh.vertices = meshinfo.verts;
            mesh.triangles = meshinfo.index;

            //The diffuse shader wants uvs so just fill with a empty array, there not actually used
            mesh.uv = new Vector2[mesh.vertices.Length];
            mesh.RecalculateNormals();

            if (coloured.colour == 0) continue;
            if (coloured.main) {
                main = true;
                //gameObject.GetComponent<MeshCollider>().convex = true;
                gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
                gameObject.GetComponent<MeshFilter>().mesh = mesh;   
                /*foreach (Transform trans in transform) {
                    Vector3 scale = trans.localScale;
                    scale.x *= transform.localScale.x;
                    scale.y *= transform.localScale.y;
                    scale.z *= transform.localScale.z;
                    trans.localScale = scale;
                }
                transform.localScale = Vector3.one;*/
            } else {
                GameObject m_mesh = new GameObject("Fragment" + coloured.colour);
                m_mesh.AddComponent<MeshFilter>();
                m_mesh.AddComponent<MeshRenderer>();
                m_mesh.AddComponent<MeshCollider>();
                m_mesh.GetComponent<MeshCollider>().convex = true;
                m_mesh.AddComponent<Rigidbody>();
                //m_mesh.rigidbody.isKinematic = true;
                m_mesh.rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                m_mesh.renderer.material = m_material;
                m_mesh.GetComponent<MeshCollider>().sharedMesh = mesh;
                m_mesh.GetComponent<MeshFilter>().mesh = mesh;
            }

            messages.Add("Building " + colour.colour + " " + colour.main + ": " + (Time.realtimeSinceStartup - time));
            time = Time.realtimeSinceStartup;
        }

        MeshInfo fmeshinfo = convexDriver.StartMeshing(allFrags);

        Mesh fmesh = new Mesh();
        Colouring fcoloured = fmeshinfo.colour;

        fmesh.vertices = fmeshinfo.verts;
        fmesh.triangles = fmeshinfo.index;

        //The diffuse shader wants uvs so just fill with a empty array, there not actually used
        fmesh.uv = new Vector2[fmesh.vertices.Length];
        fmesh.RecalculateNormals();
        GameObject fm_mesh = new GameObject("Fragment" + allFrags.colour);
        fm_mesh.AddComponent<MeshFilter>();
        fm_mesh.AddComponent<MeshRenderer>();
        fm_mesh.AddComponent<MeshCollider>();
        fm_mesh.GetComponent<MeshCollider>().convex = true;
        fm_mesh.AddComponent<Rigidbody>();
        fm_mesh.rigidbody.isKinematic = true;
        fm_mesh.rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        fm_mesh.renderer.material = m_material;
        fm_mesh.GetComponent<MeshCollider>().sharedMesh = fmesh;
        fm_mesh.GetComponent<MeshFilter>().mesh = fmesh;



        if (!main) {
            GameObject.Destroy(gameObject);
        }

        messages.Add("Done :" + (Time.realtimeSinceStartup - startTime));

        foreach (string str in messages) {
            Debug.Log(str);
        }

        done = true;
    }

    void OnDrawGizmos() {
        Gizmos.color = new Color(1.0f, 0.0f, 0.0f, .5f);
        if (done) {
            //DrawMeshShell();
        }
    }

    void DrawMeshShell() {
        if (grid != null) {
            if (drawMeshShell && (grid != null)) {
                var cubeSize = new Vector3(cubeSide, cubeSide, cubeSide);
                var gridSize = grid.GetSize();
                for (short x = 0; x < gridSize.x; ++x) {
                    for (short y = 0; y < gridSize.y; ++y) {
                        for (short z = 0; z < gridSize.z; ++z) {
                            var cubeCenter = grid.GetAABCCenterFromGridCenter(x, y, z) +
                                    grid.GetCenter() +
                                        meshShellPositionFromObject;
                            if (grid.IsAABCSet(x, y, z)) {
                                Color color;
                                if (colors.TryGetValue(colouring[x, y, z], out color)) {
                                    Gizmos.color = color;
                                    Gizmos.DrawCube(cubeCenter, cubeSize);
                                }
                            } else if (drawEmptyCube) {
                                Gizmos.color = new Color(0f, 1f, 0f, 1f);
                                Gizmos.DrawCube(cubeCenter, cubeSize);
                            }
                        }
                    }
                }
            }
        }
    }
}
