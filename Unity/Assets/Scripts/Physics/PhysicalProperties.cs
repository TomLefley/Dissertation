using UnityEngine;
using System.Collections;

public class PhysicalProperties : MonoBehaviour {

    public float strength;
    public float brittleness;
    public float mass;

    public void Update() {
        mass = rigidbody.mass;
    }

    public float doesBreak(float force) {
        return force - strength;
    }

}
