using UnityEngine;
using System.Collections;

public class PhysicalProperties : MonoBehaviour {

    public float strength;

    public float doesBreak(float force) {
        return force - strength;
    }

}
