using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Roll : MonoBehaviour {

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {
		UpdateGO(transform);
	}

    // Update is called once per frame
    void UpdateGO(Transform root)
    {
        float c_rotSpeed = 30.0f;
        float elapse = Time.deltaTime;

        Queue<Transform> travers = new Queue<Transform>();
        travers.Enqueue(root);
        while (travers.Count > 0)
        {
            Transform tran = travers.Dequeue();
            //tran.RotateAround(tran.position, new Vector3(0, 1, 0), elapse * c_rotSpeed);
            foreach (Transform child in tran)
            {
                //Transform tranP = child.parent;
                //child.RotateAround(tranP.position, new Vector3(0, 1, 0), elapse * c_rotSpeed);
                child.RotateAround(child.position, new Vector3(0, 1, 0), elapse * c_rotSpeed);                
                travers.Enqueue(child);
            }
        }
    }
}
