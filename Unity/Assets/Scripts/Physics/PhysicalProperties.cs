using UnityEngine;
using System.Collections;

public class PhysicalProperties : MonoBehaviour {

    public static float threshold = 0;

    public float brittleness;
    public int mass;

    private float acceleration;

    public float doesBreak(float momentum) {
        return Mathf.Max(0f, (brittleness * momentum) - threshold);
    }

    public Vector3 getVelocity() {
        return gameObject.rigidbody.velocity;
    }

    public float getMomentum() {
        return getVelocity().magnitude * mass;
    }
}
