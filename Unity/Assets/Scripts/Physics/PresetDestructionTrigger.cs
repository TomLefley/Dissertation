using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Voxelisation {
    public class PresetDestructionTrigger : MonoBehaviour {

        public GameObject gameObjectToVoxelise;
        public List<Mesh> meshes;

        // Use this for initialization
        void Start() {

        }

        // Update is called once per frame
        void Update() {

        }

        void OnTriggerEnter(Collider collider) {

            float time = Time.realtimeSinceStartup;
            Debug.Log("Start " + (Time.realtimeSinceStartup - time));

            foreach (Mesh m in meshes) {

                Mesh mesh = new Mesh();

                mesh.vertices = m.vertices;
                mesh.triangles = m.triangles;

                mesh.uv = new Vector2[mesh.vertices.Length];
                mesh.RecalculateNormals();

                GameObject m_mesh = new GameObject("Fragment");
                m_mesh.AddComponent<MeshFilter>();
                m_mesh.AddComponent<MeshRenderer>();
                m_mesh.AddComponent<MeshCollider>();

                m_mesh.AddComponent<Rigidbody>();
                m_mesh.rigidbody.isKinematic = true;
                m_mesh.rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                m_mesh.GetComponent<MeshCollider>().sharedMesh = mesh;

                m_mesh.GetComponent<MeshFilter>().mesh = mesh;

            }

            Debug.Log("Finished " + (Time.realtimeSinceStartup - time));
            time = Time.realtimeSinceStartup;


        }
    }
}