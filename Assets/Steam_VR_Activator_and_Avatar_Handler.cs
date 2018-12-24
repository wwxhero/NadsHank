using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

public class Steam_VR_Activator_and_Avatar_Handler : MonoBehaviour {
    private GameObject pedestrian;
    public GameObject CameraRig;
    private bool configuration;
    private VRIK ik;
    private GameObject Head, Pelvis, Left_Hand, Right_Hand, Left_Foot, Right_Foot;
    

	// Use this for initialization
	void Start () {
        configuration = false;
	}
	
	// Update is called once per frame
	void Update () {
		if (pedestrian == null)
        {
            pedestrian = GameObject.Find("Sim3");
        }

        if (pedestrian != null && configuration == false)
        {
            CameraRig.transform.position = new Vector3(pedestrian.transform.position.x, 0f, pedestrian.transform.position.z);
            configuration = true;
        }
    }
}
