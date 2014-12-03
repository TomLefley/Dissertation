public class VoxelisationJob : ThreadedJob {

    /*Transform gameObjTransf;
    float startTime;
    Vector3[] meshVertices;
    int[] meshTriangles;
    int meshTrianglesCount;

    protected override void ThreadFunction() {
        // Do your threaded task. DON'T use the Unity API here
        float[] g_Vertices = new float[meshTriangles.Length * 3];
        int[] g_Indices = new int[meshTriangles.Length];

        ComputeBuffer g_bufVertices = new ComputeBuffer(meshTriangles.Length * 3, sizeof(float));
        ComputeBuffer g_bufIndices = new ComputeBuffer(meshTriangles.Length, sizeof(int));

        if (debug) {
            Debug.Log("Start:");
            Debug.Log("Time: " + startTime);
            Debug.Log("		Mesh Description: ");
            Debug.Log("Name: " + gameObjMesh.name);
            Debug.Log("Triangles: " + meshTrianglesCount);
            Debug.Log("Local AABB size: " + gameObjMesh.bounds.size);
            Debug.Log("		AABCGrid Description:");
            Debug.Log("Size: " + width + ',' + height + ',' + depth);
        }

        // For each triangle, perform SAT intersection check with the AABCs within the triangle AABB.
        for (int i = 0; i < meshTrianglesCount; i++) {

            g_Indices[i * 3] = 9 * i;
            g_Indices[i * 3 + 1] = 9 * i + 3;
            g_Indices[i * 3 + 2] = 9 * i + 6;

            g_Vertices[i * 9 + 0] = gameObjTransf.TransformPoint(meshVertices[meshTriangles[i * 3]]).x;
            g_Vertices[i * 9 + 1] = gameObjTransf.TransformPoint(meshVertices[meshTriangles[i * 3]]).y;
            g_Vertices[i * 9 + 2] = gameObjTransf.TransformPoint(meshVertices[meshTriangles[i * 3]]).z;

            g_Vertices[i * 9 + 3] = gameObjTransf.TransformPoint(meshVertices[meshTriangles[i * 3 + 1]]).x;
            g_Vertices[i * 9 + 4] = gameObjTransf.TransformPoint(meshVertices[meshTriangles[i * 3 + 1]]).y;
            g_Vertices[i * 9 + 5] = gameObjTransf.TransformPoint(meshVertices[meshTriangles[i * 3 + 1]]).z;

            g_Vertices[i * 9 + 6] = gameObjTransf.TransformPoint(meshVertices[meshTriangles[i * 3 + 2]]).x;
            g_Vertices[i * 9 + 7] = gameObjTransf.TransformPoint(meshVertices[meshTriangles[i * 3 + 2]]).y;
            g_Vertices[i * 9 + 8] = gameObjTransf.TransformPoint(meshVertices[meshTriangles[i * 3 + 2]]).z;

        }

        g_bufIndices.SetData(g_Indices);
        g_bufVertices.SetData(g_Vertices);

        int kernel = shader.FindKernel("CS_VoxelizeSolid");

        shader.SetBuffer(kernel, "g_bufIndices", g_bufIndices);
        shader.SetBuffer(kernel, "g_bufVertices", g_bufVertices);

        int numThreads = meshTrianglesCount;
        int threadsPerBlock = 256;

        shader.Dispatch(kernel, 256, (numThreads + (threadsPerBlock * 256 - 1)) / (threadsPerBlock * 256), 1);

        kernel = shader.FindKernel("CS_VoxelizeSolid_Propagate");

        numThreads = g_strideY;
        threadsPerBlock = 256;

        pd3dImmediateContext->Dispatch(256, (numThreads + (threadsPerBlock * 256 - 1)) / (threadsPerBlock * 256), 1);

        //ComputeBuffer g_rwbufVoxels = new ComputeBuffer();

    }
    protected override void OnFinished() {
        // This is executed by the Unity main thread when the job is finished
        if (debug) {
            Debug.Log("Grid Evaluation Ended!");
            Debug.Log("Time spent: " + (Time.realtimeSinceStartup - startTime) + "s");
            Debug.Log("End: ");
        }
    }*/
}