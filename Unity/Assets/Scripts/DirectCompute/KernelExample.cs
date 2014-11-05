using UnityEngine;
using System.Collections;

public class KernelExample : MonoBehaviour {

    public ComputeShader shader;

    void Start() {
        shader.Dispatch(0, 1, 1, 1);
    }

}