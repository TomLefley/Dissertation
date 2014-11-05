using UnityEngine;
using System.Collections;

public class KernelExample : MonoBehaviour {

    public ComputeShader shader;

    void Start() {
        ComputeBuffer buffer = new ComputeBuffer(4 * 2, sizeof(int));

        shader.SetBuffer(0, "buffer1", buffer);

        shader.Dispatch(0, 2, 1, 1);

        int[] data = new int[4 * 2];

        buffer.GetData(data);

        for (int i = 0; i < 4 * 2; i++)
            Debug.Log(data[i]);

        buffer.Release();
    }

}