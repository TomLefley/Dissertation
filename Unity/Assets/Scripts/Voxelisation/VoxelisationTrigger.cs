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

        gameObjectToVoxelise.BroadcastMessage("StartVoxelise");
        

    }
}
