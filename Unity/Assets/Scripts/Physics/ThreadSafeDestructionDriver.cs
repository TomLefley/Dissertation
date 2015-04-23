using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThreadSafeDestructionDriver : MonoBehaviour {

    public bool drawMeshShell = true;
    public bool drawMeshInside = true;
    public bool drawEmptyCube = false;
    public bool includeChildren = true;
    public bool createMultipleGrids = true;
    public Vector3 meshShellPositionFromObject = Vector3.zero;
    public float cubeSide = 0.1f;

    public bool hollow;

    public bool debug;
    public ComputeShader shader;

    public List<string> messages = new List<string>();
    public List<string> csvList = new List<string>();

    ThreadSafeVoxelisation.ThreadSafeVoxelisationDriver voxelisationDriver;
    ThreadSafeDestruction destruction;
    ThreadSafeMarchingCubesDriver marchingDriver;
    ThreadSafeConvexHullDriver convexDriver;
    ThreadSafeSplitMesh splitMesh;
    ThreadSafeHoleFill holefill;

    ThreadSafeVoxelisation.Voxelization.AABCGrid grid;

    Dictionary<short, Fragment> fragments;
    short[, ,] colouring;

    public Material m_material;

    Dictionary<short, Color> colors = new Dictionary<short, Color>();

    bool done = false;

    void Start() {
        voxelisationDriver = new ThreadSafeVoxelisation.ThreadSafeVoxelisationDriver(drawMeshInside, includeChildren, createMultipleGrids, cubeSide, debug, shader);
        destruction = new ThreadSafeDestruction();
        marchingDriver = new ThreadSafeMarchingCubesDriver();
        convexDriver = new ThreadSafeConvexHullDriver();
        splitMesh = new ThreadSafeSplitMesh();
        holefill = new ThreadSafeHoleFill();
    }

    //Unused attempt at multithreading
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

                Fragment itColouring;
                bool found = fragments.TryGetValue(i, out itColouring);

                if (found) {
                    meshinfos.Add(new ThreadSafeMarchingCubesDriver().StartMarching(colouring, itColouring, grid));
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
                    Fragment coloured = meshinfo.colour;

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

    public void SplitDestroy(Vector3 hitPoint, float hitForce, PhysicalProperties physicalProperties) {

        messages.Add("Starting");
        float time = Time.realtimeSinceStartup;
        float startTime = time;

        voxelisationDriver.StartVoxelise(gameObject);


        messages.Add("Voxelisation: " + (Time.realtimeSinceStartup - time));
        csvList.Add((Time.realtimeSinceStartup - time)+" ");
        time = Time.realtimeSinceStartup;

        grid = voxelisationDriver.GetGrid();

        fragments = destruction.Fragment(grid, hitPoint, hitForce, physicalProperties);

        messages.Add("Destruction: " + (Time.realtimeSinceStartup - time));
        csvList.Add((Time.realtimeSinceStartup - time) + " ");
        time = Time.realtimeSinceStartup;

        colouring = destruction.getVoronoiDiagram();

        foreach (Fragment c in fragments.Values) {
            c.vertices.Clear();
        }

        short[, ,] borderColouring = new short[colouring.GetLength(0), colouring.GetLength(1), colouring.GetLength(2)];

        List<Vector3> vectors = new List<Vector3>();

        for (int i = 0; i < colouring.GetLength(0); i++) {
            for (int j = 0; j < colouring.GetLength(1); j++) {
                for (int k = 0; k < colouring.GetLength(2); k++) {
                    short c = colouring[i, j, k];
                    bool neighbours = false;
                    bool exterior = false;
                    if (c == 0) continue;
                    for (int x = -1; x <= 1; x++) {
                        for (int y = -1; y <= 1; y++) {
                            for (int z = -1; z <= 1; z++) {
                                if (i + x < 0 || j + y < 0 || k + z < 0) continue;
                                if (i + x >= colouring.GetLength(0) || j + y >= colouring.GetLength(1) || k + z >= colouring.GetLength(2)) continue;
                                if (colouring[i + x, j + y, k + z] != c && colouring[i + x, j + y, k + z] != 0) {
                                    neighbours = true;
                                }
                                if (colouring[i + x, j + y, k + z] == 0) {
                                    exterior = true;
                                }
                            }
                        }
                    }
                    if (exterior) {
                        vectors.Add(new Vector3(i, j, k));

                    }
                    if (neighbours) {
                        borderColouring[i, j, k] = c;
                        
                    }
                    if (neighbours && exterior) {
                        Fragment colour;
                        if (fragments.TryGetValue(c, out colour)) {
                            colour.vertices.Add(new Vector3(i, j, k));
                        }
                    }
                }
            }
        }

        KDTree tree = KDTree.MakeFromPoints(vectors.ToArray());

        messages.Add("KDTree: " + (Time.realtimeSinceStartup - time));
        time = Time.realtimeSinceStartup;

        MeshInfo original = new MeshInfo(gameObject.GetComponent<MeshFilter>().mesh.vertices, gameObject.GetComponent<MeshFilter>().mesh.triangles, new Fragment(0));

        Dictionary<short, MeshInfo> meshes = splitMesh.Split(original, tree, vectors, colouring, grid.GetSize());

        messages.Add("Split: " + (Time.realtimeSinceStartup - time));
        csvList.Add((Time.realtimeSinceStartup - time) + " ");
        time = Time.realtimeSinceStartup;


        if (!hollow) {

            csvList.Add("Meshing ");

            foreach (Fragment colour in fragments.Values) {
                if (colour == null) continue;

                MeshInfo meshinfo, march;

                MeshInfo parent;
                bool found = meshes.TryGetValue(colour.colour, out parent);

                colors.Add(colour.colour, new Color(Random.value, Random.value, Random.value));

                if (found) {
                    List<Vector3> edges = parent.colour.vertices;
                    List<Vector3> exterior = new List<Vector3>();
                    foreach (Vector3 v in colour.vertices) {
                        exterior.Add(toWorldSpace(v));
                    }

                    Vector3 voxelMid = new Vector3((colour.maxX-colour.minX)/2, (colour.maxY-colour.minY)/2, (colour.maxZ-colour.minZ)/2);
                    voxelMid.x = voxelMid.x / grid.GetSize().x;
                    voxelMid.y = voxelMid.y / grid.GetSize().y;
                    voxelMid.z = voxelMid.z / grid.GetSize().z;
                    voxelMid *= 2;

                    KDTree surface = KDTree.MakeFromPoints(edges.ToArray());
                    //meshinfo = holefill.Stitch(edges, colour, exterior);

                    meshinfo = marchingDriver.StartMarchingClamp(borderColouring, colour, grid, surface, edges);
                } else {
                    meshinfo = marchingDriver.StartMarching(borderColouring, colour, grid);
                    march = marchingDriver.StartMarching(borderColouring, colour, grid);
                }
                //meshinfo = convexDriver.StartMeshing(colour);

                /*for (int c = 0; c < meshinfo.verts.Length; c++) {
                    meshinfo.verts[c] = new Vector3(meshinfo.verts[c].x / transform.lossyScale.x, meshinfo.verts[c].y / transform.lossyScale.y, meshinfo.verts[c].z / transform.lossyScale.z);
                }*/

                messages.Add("Meshing " + colour.colour + ": " + (Time.realtimeSinceStartup - time));
                csvList.Add((Time.realtimeSinceStartup - time) + " ");
                time = Time.realtimeSinceStartup;

                if (colour.colour == 0) {
                    continue;
                } else {
                     if (found) {
                        List<Vector3> verts = new List<Vector3>(parent.verts);
                        int count = verts.Count;
                        verts.AddRange(meshinfo.verts);

                        List<int> indices = new List<int>(parent.index);
                        foreach (int i in meshinfo.index) {
                            indices.Add(i + count);
                        }

                        /*count = verts.Count;
                        verts.AddRange(march.verts);

                        foreach (int i in march.index) {
                            indices.Add(i + count);
                        }*/

                        parent.verts = verts.ToArray();
                        parent.index = indices.ToArray();
                        parent.colour.mass = meshinfo.colour.mass;

                    } else {
                        meshes.Add(colour.colour, meshinfo);
                    }

                }

            }
        }

        csvList.Add("Building ");

        foreach (MeshInfo meshinfo in meshes.Values) {

            Mesh mesh = new Mesh();
            Fragment coloured = meshinfo.colour;

            fragments.TryGetValue(coloured.colour, out coloured);

            if (coloured.colour == 0) continue;

            mesh.vertices = meshinfo.verts;
            mesh.triangles = meshinfo.index;

            //The diffuse shader wants uvs so just fill with a empty array, they're not actually used
            mesh.uv = new Vector2[mesh.vertices.Length];
            mesh.RecalculateNormals();

            Mesh mesh1 = new Mesh();

            if (hollow) {
                List<int> indices = new List<int>(meshinfo.index);

                for (int i = 0; i < meshinfo.index.Length; i += 3) {
                    indices.Add(meshinfo.index[i + 2]);
                    indices.Add(meshinfo.index[i + 1]);
                    indices.Add(meshinfo.index[i + 0]);
                }

                meshinfo.index = indices.ToArray();

                /*List<Vector3> normals = new List<Vector3>(mesh.normals);
                for (int i = 0; i < mesh.normals.Length; i++)
                    normals.Add(-normals[i]);*/

                mesh1.vertices = meshinfo.verts;
                mesh1.triangles = meshinfo.index;

                //The diffuse shader wants uvs so just fill with a empty array, they're not actually used
                mesh1.uv = new Vector2[mesh.vertices.Length];

                mesh1.normals = mesh.normals;
            }

            List<Vertex3> vertex = new List<Vertex3>();
            foreach (Vector3 v in mesh.vertices) {
                vertex.Add(new Vertex3(v.x, v.y, v.z));
            }

            //coloured.vertices = vertex;

            //MeshInfo convex = convexDriver.StartMeshing(coloured);

            Mesh mesh2 = new Mesh();

            mesh2.vertices = meshinfo.verts;
            mesh2.triangles = meshinfo.index;


            GameObject m_mesh = new GameObject("Fragment" + coloured.colour);
            m_mesh.AddComponent<MeshFilter>();
            m_mesh.AddComponent<MeshRenderer>();
            m_mesh.AddComponent<MeshCollider>();
            
            m_mesh.AddComponent<Rigidbody>();
            m_mesh.rigidbody.isKinematic = true;
            m_mesh.rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            m_mesh.rigidbody.mass = coloured.mass;
            m_mesh.renderer.material = m_material;

            if (mesh2.triangles.Length / 3 >= 255) {
                m_mesh.GetComponent<MeshCollider>().sharedMesh = mesh;
            } else {
                m_mesh.GetComponent<MeshCollider>().sharedMesh = mesh2;
                m_mesh.GetComponent<MeshCollider>().convex = true;
            }
            
            m_mesh.GetComponent<MeshFilter>().mesh = hollow ? mesh1 : mesh;

            m_mesh.transform.localScale = transform.localScale;

            messages.Add("Built " + coloured.colour + ": " + (Time.realtimeSinceStartup - time));
            csvList.Add((Time.realtimeSinceStartup - time) + " ");
            time = Time.realtimeSinceStartup;
        }

        messages.Add("Done:" + (Time.realtimeSinceStartup - startTime));

        string csv = "";

        foreach (string str in csvList) {
            csv += str;
        }

        foreach (string str in messages) {
            Debug.Log(str);
        }

        //Debug.Log(csv);

        done = true;

        GameObject.Destroy(gameObject);
    }

    private Vector3 toWorldSpace(Vector3 voxelSpace) {
        ThreadSafeVoxelisation.GridSize gridSize = grid.GetSize();
        float x = (voxelSpace.x - (gridSize.x * 0.5f)) * gridSize.side/2;
        float y = (voxelSpace.y - (gridSize.y * 0.5f)) * gridSize.side/2;
        float z = (voxelSpace.z - (gridSize.z * 0.5f)) * gridSize.side/2;
        return new Vector3(x, y, z);
    }

    private Vector3 toVoxelSpace(Vector3 worldSpace) {
        float side = grid.GetSize().side;

        return new Vector3(worldSpace.x / side, worldSpace.y / side, worldSpace.z / side);
    }


    void OnDrawGizmos() {
        Gizmos.color = new Color(1.0f, 0.0f, 0.0f, .5f);
        if (done) {
            //DrawMeshShell();
            //DrawVoxels();
        }
    }

    void DrawVoxels() {
        var cubeSize = new Vector3(cubeSide, cubeSide, cubeSide);
        var gridSize = grid.GetSize();
        foreach (Fragment c in fragments.Values) {
            foreach (Vector3 v in c.vertices) {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(new Vector3((float)v.x*2 / gridSize.x, (float)v.y*2 / gridSize.y, (float)v.z*2 / gridSize.z), cubeSize);
            }
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
