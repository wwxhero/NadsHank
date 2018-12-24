using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;
using RootMotion.Demos;

public class model_and_Steam_VR_Controller : MonoBehaviour {
    private GameObject pedestrian, cameraRig, actual_Targets, other_Targets;
    public Transform actualTargets, otherTargets;
    public Transform CameraRig;
    public bool configuration, tracker_configuration;
    private VRIKCalibrationController ik;
    private VRIK ik1;
    public GameObject Head, Pelvis, Left_Hand, Right_Hand, Left_Foot, Right_Foot, Head_ik, Pelvis_ik, Left_Hand_ik, Right_Hand_ik, Left_Foot_ik, Right_Foot_ik, left_hand_target_vrik_calibrator, right_hand_target_vrik_calibrator, left_foot_target_vrik_calibrator, right_foot_target_vrik_calibrator, head_target_vrik_calibrator, pelvis_target_vrik_calibrator;

    // Use this for initialization
    void Start () {
        configuration = false;
        tracker_configuration = false;
    }
	
	// Update is called once per frame
	void Update () {
        if (pedestrian == null)
        {
            pedestrian = GameObject.Find("Sim3");
        }

        if (pedestrian != null && configuration == false)
        {
            cameraRig = Instantiate(CameraRig, new Vector3(pedestrian.transform.position.x, 0f, pedestrian.transform.position.z), Quaternion.identity).gameObject;
            cameraRig.name = "[CameraRig]";
            actual_Targets = Instantiate(actualTargets, new Vector3(pedestrian.transform.position.x, 0f, pedestrian.transform.position.z), Quaternion.identity).gameObject;
            other_Targets = Instantiate(otherTargets, new Vector3(pedestrian.transform.position.x, 0f, pedestrian.transform.position.z), Quaternion.identity).gameObject;
            actual_Targets.name = "Actual Targets";
            other_Targets.name = "Other Targets";
            pedestrian.AddComponent<Tracker_Automatic_Calibrator>();
            pedestrian.AddComponent<TrackerCalibrationController>();
            //pedestrian.AddComponent<Calibration_Script>();

            ik1 = pedestrian.GetComponent<VRIK>();
            //Head_ik = GameObject.Find("[CameraRig]/Camera (eye)/Head");
            //Pelvis_ik = GameObject.Find("Other Targets/Pelvis_Bone_Tracker/Pelvis");
            //Left_Hand_ik = GameObject.Find("Other Targets/Left_Hand_Tracker/Left Hand");
            //Right_Hand_ik = GameObject.Find("Other Targets/Right_Hand_Tracker/Right Hand");
            //Left_Foot_ik = GameObject.Find("Other Targets/Left_Foot_Tracker/Left Foot");
            //Right_Foot_ik = GameObject.Find("Other Targets/Right_Foot_Tracker/Right Foot");

            ik = this.gameObject.GetComponent<VRIKCalibrationController>();
            Head = GameObject.Find("[CameraRig]/Camera (eye)");
            Pelvis = GameObject.Find("Other Targets/Pelvis_Bone_Tracker");
            Left_Hand = GameObject.Find("Other Targets/Left_Hand_Tracker");
            Right_Hand = GameObject.Find("Other Targets/Right_Hand_Tracker");
            Left_Foot = GameObject.Find("Other Targets/Left_Foot_Tracker");
            Right_Foot = GameObject.Find("Other Targets/Right_Foot_Tracker");

            //ik1.solver.spine.headTarget = Head_ik.transform;
            //ik1.solver.spine.pelvisTarget = Pelvis_ik.transform;
            //ik1.solver.spine.pelvisPositionWeight = 1f;
            //ik1.solver.spine.pelvisRotationWeight = 1f;

            //ik1.solver.leftArm.target = Left_Hand_ik.transform;
            //ik1.solver.rightArm.target = Right_Hand_ik.transform;

            //ik1.solver.leftLeg.target = Left_Foot_ik.transform;
            //ik1.solver.rightLeg.target = Right_Foot_ik.transform;
            //ik1.solver.leftLeg.positionWeight = 1f;
            //ik1.solver.leftLeg.rotationWeight = 1f;
            //ik1.solver.rightLeg.positionWeight = 1f;
            //ik1.solver.rightLeg.rotationWeight = 1f;

            ik.ik = ik1;
            ik.headTracker = Head.transform;
            ik.bodyTracker = Pelvis.transform;
            ik.leftHandTracker = Left_Hand.transform;
            ik.leftFootTracker = Left_Foot.transform;
            ik.rightHandTracker = Right_Hand.transform;
            ik.rightFootTracker = Right_Foot.transform;

            //ik.data.head.used = true;
            //ik.data.leftHand.used = true;
            //ik.data.rightHand.used = true;
            //ik.data.leftFoot.used = true;
            //ik.data.rightFoot.used = true;
            //ik.data.leftLegGoal.used = true;
            //ik.data.rightLegGoal.used = true;
            //ik.data.pelvis.used = true;

            configuration = true;

            ////ik.solver.spine.maintainPelvisPosition = 0f;
        }
        
        if (ik.calibration_done == true)
        {
            cameraRig.transform.eulerAngles = new Vector3(cameraRig.transform.eulerAngles.x,
                                                            90f,
                                                            cameraRig.transform.eulerAngles.z);

            other_Targets.transform.eulerAngles = new Vector3(other_Targets.transform.eulerAngles.x,
                                                            90f,
                                                            other_Targets.transform.eulerAngles.z);

            var new_scale = ik.data.scale;
            if (new_scale > 1f)
            {
                var difference = new_scale - 1f;
                var new_difference = difference * 3.3f;
                var updated_scale = 3.3f + new_difference;
                pedestrian.transform.localScale = new Vector3(updated_scale, updated_scale, updated_scale);
            }

            else if (new_scale < 1f && new_scale > 0f)
            {
                var difference = 1f - new_scale;
                var new_difference = difference * 3.3f;
                var updated_scale = 3.3f + new_difference;
                pedestrian.transform.localScale = new Vector3(updated_scale, updated_scale, updated_scale);
            }

            else
                pedestrian.transform.localScale = new Vector3(3.3f, 3.3f, 3.3f);

            cameraRig.transform.localScale = new Vector3(3.3f, 3.3f, 3.3f);
            other_Targets.transform.localScale = new Vector3(3.3f, 3.3f, 3.3f);
            ik1.solver.plantFeet = true;

            //rightHand_Updated_Rotation = rightHand.transform.rotation;
            //left_Foot_Updated_Rotation = leftLeg.transform.rotation;
            //right_Foot_Updated_Rotation = rightLeg.transform.rotation;
            //pelvis_Updated_Rotation = pelvis.transform.rotation;
            //head_Updated_Rotation = head.transform.rotation;

            //left_hand_target_vrik_calibrator = GameObject.Find(Left_Hand.name + "/Left Hand Target");
            //right_hand_target_vrik_calibrator = GameObject.Find(Right_Hand.name + "/Right Hand Target");
            //left_foot_target_vrik_calibrator = GameObject.Find(Left_Foot.name + "/Left Foot Target");
            //right_foot_target_vrik_calibrator = GameObject.Find(Right_Foot.name + "/Right Foot Target");
            //head_target_vrik_calibrator = GameObject.Find(Head.name + "/Head Target");
            //pelvis_target_vrik_calibrator = GameObject.Find(Pelvis.name + "/Pelvis Target");

            //head_target_vrik_calibrator.transform.localPosition = new Vector3(0.0f, 0.01f, -0.16f);
            //pelvis_target_vrik_calibrator.transform.localPosition = new Vector3(0.0f, -0.15f, -0.25f);
            //left_foot_target_vrik_calibrator.transform.localEulerAngles = new Vector3(-10.0f, 165.0f, -40.0f);
            //left_hand_target_vrik_calibrator.transform.localEulerAngles = new Vector3(-150.0f, 90.0f, 180.0f);
            //right_foot_target_vrik_calibrator.transform.localEulerAngles = new Vector3(175.0f, 20.0f, 160.0f);
            //right_hand_target_vrik_calibrator.transform.localEulerAngles = new Vector3(-60.0f, -90.0f, -180.0f);

            //ik1.solver.leftLeg.bendGoalWeight = 0.0f;
            //ik1.solver.rightLeg.bendGoalWeight = 0.0f;

            ik.calibration_done = false;
            tracker_configuration = true;
        }

        if (ik1.solver.leftLeg.bendGoalWeight == 1.0f)
            ik1.solver.leftLeg.bendGoalWeight = 0.0f;
        if (ik1.solver.rightLeg.bendGoalWeight == 1.0f)
            ik1.solver.rightLeg.bendGoalWeight = 0.0f;
    }
}
