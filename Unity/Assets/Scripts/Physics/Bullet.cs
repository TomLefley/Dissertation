﻿using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour {

    public Vector3 force;

	// Use this for initialization
	void Start () {

        rigidbody.isKinematic = true;
        //rigidbody.AddForce(force);
	}
	
	// Update is called once per frame
	void Update () {

        if (Input.GetKeyUp("space")) {
            rigidbody.isKinematic = false;
            rigidbody.AddForce(force);
        }
	}
}
