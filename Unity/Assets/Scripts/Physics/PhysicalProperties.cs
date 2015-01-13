using UnityEngine;
using System.Collections;

public class PhysicalProperties : MonoBehaviour {

    public static float threshold;

    public float brittleness;
    public int mass;

    public float acceleration;

    public float doesBreaK(float force) {
        return Mathf.Max(0f, (brittleness * force) - threshold);
    }

    public Vector3 getVelocity() {
        return gameObject.rigidbody.velocity;
    }

    public IEnumerator setDirection() {
        Vector3 velocity = gameObject.rigidbody.velocity;
        yield return null;
        Vector3 velocity2 = gameObject.rigidbody.velocity;
        acceleration = (velocity2 - velocity).magnitude / Time.deltaTime;
    }

    public float getForce() {
        return mass * acceleration;
    }
}
