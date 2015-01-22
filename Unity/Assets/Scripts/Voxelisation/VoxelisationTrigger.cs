using UnityEngine;
using System.Collections;

public class VoxelisationTrigger : MonoBehaviour {

    public GameObject gameObjectToVoxelise;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnTriggerEnter(Collider collider) {

        if (collider.gameObject.Equals(gameObjectToVoxelise)) return;

        PhysicalProperties gameObjectToVoxelisePP = gameObjectToVoxelise.GetComponent<PhysicalProperties>();
        PhysicalProperties collidingPP = collider.gameObject.GetComponent<PhysicalProperties>();

        float radius = gameObjectToVoxelisePP.doesBreak(collidingPP.getMomentum());

        RaycastHit willCollidePoint;

        Ray ray = new Ray(collider.transform.position, collider.rigidbody.velocity);

        Physics.Raycast(ray, out willCollidePoint);

        Rigidbody willCollideWith = willCollidePoint.rigidbody;

        Vector3 velocity1 = willCollideWith ? willCollideWith.velocity : Vector3.zero;

        Vector3 velocity2 = collider.rigidbody.velocity;

        float mass1 = gameObjectToVoxelisePP.mass;

        Debug.Log(mass1);

        float mass2 = collider.rigidbody.mass;

        Debug.Log(mass2);

        Vector3 willCollideNormal = willCollidePoint.normal;
        willCollideNormal.Normalize();
        willCollideNormal*=2;

        float exertedForce = Vector3.Dot(willCollideNormal,(mass2 * velocity1 - mass1 * velocity2)) / (mass1 + mass2);

        exertedForce /= Time.fixedDeltaTime;

        Debug.Log(exertedForce);

        //if (radius == 0f) return;

        Debug.Log(radius);

        gameObjectToVoxelise.BroadcastMessage("StartVoxelise");
        

    }
}
