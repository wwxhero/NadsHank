using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

public class model_and_Steam_VR_Controller : MonoBehaviour {
    private GameObject pedestrian, cameraRig, actual_Targets, other_Targets;
    public Transform actualTargets, otherTargets;
    public Transform CameraRig;
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
            pedestrian = GameObject.Find("Sim3");

        if (pedestrian != null && configuration == false)
        {
            cameraRig = Instantiate(CameraRig, new Vector3(pedestrian.transform.position.x, 0f, pedestrian.transform.position.z), Quaternion.identity).gameObject;
            cameraRig.name = "[CameraRig]";
            actual_Targets = Instantiate(actualTargets, new Vector3(pedestrian.transform.position.x, 0f, pedestrian.transform.position.z), Quaternion.identity).gameObject;
            other_Targets = Instantiate(otherTargets, new Vector3(pedestrian.transform.position.x, 0f, pedestrian.transform.position.z), Quaternion.identity).gameObject;
            actual_Targets.name = "Actual Targets";
            other_Targets.name = "Other Targets";
            pedestrian.AddComponent<TrackerCalibrationController>();
            pedestrian.AddComponent<Calibration_Script>();
            
            configuration = true;

            ik = pedestrian.GetComponent<RootMotion.FinalIK.VRIK>();
            Head = GameObject.Find("[CameraRig]/Camera (eye)/Head");
            Pelvis = GameObject.Find("Other Targets/Pelvis_Bone_Tracker/Pelvis");
            Left_Hand = GameObject.Find("Other Targets/Left_Hand_Tracker/Left Hand");
            Right_Hand = GameObject.Find("Other Targets/Right_Hand_Tracker/Right Hand");
            Left_Foot = GameObject.Find("Other Targets/Left_Foot_Tracker/Left Foot");
            Right_Foot = GameObject.Find("Other Targets/Right_Foot_Tracker/Right Foot");

            ik.solver.spine.headTarget = Head.transform;
            ik.solver.spine.pelvisTarget = Pelvis.transform;
            ik.solver.spine.pelvisPositionWeight = 1f;
            ik.solver.spine.pelvisRotationWeight = 1f;

            ik.solver.leftArm.target = Left_Hand.transform;
            ik.solver.rightArm.target = Right_Hand.transform;

            ik.solver.leftLeg.target = Left_Foot.transform;
            ik.solver.rightLeg.target = Right_Foot.transform;
            ik.solver.leftLeg.positionWeight = 1f;
            ik.solver.leftLeg.rotationWeight = 1f;
            ik.solver.rightLeg.positionWeight = 1f;
            ik.solver.rightLeg.rotationWeight = 1f;
        }
    }
}
