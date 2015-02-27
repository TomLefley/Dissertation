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

    public bool hollow;

    public bool debug;
    public ComputeShader shader;

    public List<string> messages = new List<string>();

    ThreadedVoxelisation.ThreadedVoxelisationDriver voxelisationDriver;
    ThreadedDestruction destruction;
    ThreadedMarchingCubesDriver marchingDriver;
    ThreadedConvexHullDriver convexDriver;
    ConstructiveSolidGeometry csg;
    ThreadedSplitMesh splitMesh;

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
        splitMesh = new ThreadedSplitMesh();
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
                    meshinfos.Add(new ThreadedMarchingCubesDriver().StartMarching(colouring, itColouring, grid, null, null));
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

        List<Vector3> vectors = new List<Vector3>();

        for (int i = 0; i < colouring.GetLength(0); i++) {
            for (int j = 0; j < colouring.GetLength(1); j++) {
                for (int k = 0; k < colouring.GetLength(2); k++) {
                    vectors.Add(new Vector3(i, j, k));
                }
            }
        }

        KDTree tree = KDTree.MakeFromPoints(vectors.ToArray());

        messages.Add("KDTree: " + (Time.realtimeSinceStartup - time));
        time = Time.realtimeSinceStartup;

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

        MeshInfo fmeshinfo = convexDriver.StartMeshing(allFrags);

        foreach (Colouring colour in fragments.Values) {
            if (colour == null) continue;

            MeshInfo meshinfo;

            colors.Add(colour.colour, new Color(Random.value, Random.value, Random.value));

            if (colour.main) {
                //meshinfo = convexDriver.StartMeshing(colour);
                meshinfo = marchingDriver.StartMarching(colouring, colour, grid, null, null);
                //meshinfo = csg.StartMeshing(new MeshInfo(gameObject.GetComponent<MeshFilter>().mesh.vertices, gameObject.GetComponent<MeshFilter>().mesh.triangles, colour), fmeshinfo, colour, new List<Colouring>(fragments.Values), colouring, grid.GetSize());
            } else {
                //meshinfo = convexDriver.StartMeshing(colour);
                meshinfo = marchingDriver.StartMarching(colouring, colour, grid, null, null);
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
                m_mesh.rigidbody.isKinematic = true;
                m_mesh.rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                m_mesh.renderer.material = m_material;
                m_mesh.GetComponent<MeshCollider>().sharedMesh = mesh;
                m_mesh.GetComponent<MeshFilter>().mesh = mesh;
            }

            messages.Add("Building " + colour.colour + " " + colour.main + ": " + (Time.realtimeSinceStartup - time));
            time = Time.realtimeSinceStartup;
        }

        /*Mesh fmesh = new Mesh();
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
        fm_mesh.GetComponent<MeshFilter>().mesh = fmesh;*/



        if (!main) {
            GameObject.Destroy(gameObject);
        }

        messages.Add("Done :" + (Time.realtimeSinceStartup - startTime));

        foreach (string str in messages) {
            Debug.Log(str);
        }

        done = true;
    }
    public void SplitDestroy(Vector3 hitPoint, float hitForce, PhysicalProperties physicalProperties) {

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

        foreach (Colouring c in fragments.Values) {
            c.vertices.Clear();
        }
        foreach (Colouring c in fragments.Values) {
            foreach (Vertex3 v in c.vertices) {
                Debug.Log(v.ToString());
            }
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
                    if (neighbours && !exterior) {
                        borderColouring[i, j, k] = c;
                        
                    }
                    if (neighbours && exterior) {
                        Colouring colour;
                        if (fragments.TryGetValue(c, out colour)) {
                            colour.vertices.Add(new Vertex3(i, j, k));
                        }
                    }
                }
            }
        }

        KDTree tree = KDTree.MakeFromPoints(vectors.ToArray());

        messages.Add("KDTree: " + (Time.realtimeSinceStartup - time));
        time = Time.realtimeSinceStartup;

        MeshInfo original = new MeshInfo(gameObject.GetComponent<MeshFilter>().mesh.vertices, gameObject.GetComponent<MeshFilter>().mesh.triangles, new Colouring(0));

        Dictionary<short, MeshInfo> meshes = splitMesh.Split(original, tree, vectors, colouring, grid.GetSize());

        messages.Add("Split: " + (Time.realtimeSinceStartup - time));
        time = Time.realtimeSinceStartup;


        if (!hollow) {
            foreach (Colouring colour in fragments.Values) {
                if (colour == null) continue;

                MeshInfo meshinfo;

                MeshInfo parent;
                bool found = meshes.TryGetValue(colour.colour, out parent);

                colors.Add(colour.colour, new Color(Random.value, Random.value, Random.value));

                if (found) {
                    List<Vertex3> edges = parent.colour.vertices;
                    Vector3[] vEdges = new Vector3[edges.Count];

                    for (int i = 0; i < edges.Count; i++) {
                        vEdges[i] = edges[i].ToVector3();
                    }
                    KDTree surface = KDTree.MakeFromPoints(vEdges);
                    meshinfo = marchingDriver.StartMarching(borderColouring, colour, grid, surface, vEdges);
                } else {
                    meshinfo = marchingDriver.StartMarching(borderColouring, colour, grid, null, null);
                }
                //meshinfo = convexDriver.StartMeshing(colour);

                for (int c = 0; c < meshinfo.verts.Length; c++) {
                    meshinfo.verts[c] = new Vector3(meshinfo.verts[c].x / transform.lossyScale.x, meshinfo.verts[c].y / transform.lossyScale.y, meshinfo.verts[c].z / transform.lossyScale.z);
                }

                messages.Add("Meshing " + colour.colour + " " + colour.main + ": " + (Time.realtimeSinceStartup - time));
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

                        parent.verts = verts.ToArray();
                        parent.index = indices.ToArray();

                    } else {
                        meshes.Add(colour.colour, meshinfo);
                    }

                }

            }
        }

        foreach (MeshInfo meshinfo in meshes.Values) {

            Mesh mesh = new Mesh();
            Colouring coloured = meshinfo.colour;

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
            time = Time.realtimeSinceStartup;
        }

        messages.Add("Done:" + (Time.realtimeSinceStartup - startTime));

        foreach (string str in messages) {
            Debug.Log(str);
        }

        done = true;

        GameObject.Destroy(gameObject);
    }


    void OnDrawGizmos() {
        Gizmos.color = new Color(1.0f, 0.0f, 0.0f, .5f);
        if (done) {
            //DrawMeshShell();
            DrawVoxels();
        }
    }

    void DrawVoxels() {
        var cubeSize = new Vector3(cubeSide, cubeSide, cubeSide);
        var gridSize = grid.GetSize();
        foreach (Colouring c in fragments.Values) {
            foreach (Vertex3 v in c.vertices) {
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
