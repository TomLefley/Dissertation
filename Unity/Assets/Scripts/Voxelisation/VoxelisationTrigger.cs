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

        //if (radius == 0f) return;

        Debug.Log(radius);

        gameObjectToVoxelise.BroadcastMessage("StartVoxelise");
        

    }
}
