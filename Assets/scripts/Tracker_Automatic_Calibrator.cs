using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;
using RootMotion.Demos;

public class Tracker_Automatic_Calibrator : MonoBehaviour {
    private GameObject leftHand, rightHand, leftLeg, rightLeg, pelvis, head;
    private Quaternion leftHand_Default_Rotation, left_Foot_Default_Rotation, rightHand_Default_Rotation, right_Foot_Default_Rotation, pelvis_Default_Rotation, head_Default_Rotation;
    private Quaternion leftHand_Updated_Rotation, left_Foot_Updated_Rotation, rightHand_Updated_Rotation, right_Foot_Updated_Rotation, pelvis_Updated_Rotation, head_Updated_Rotation;
    public model_and_Steam_VR_Controller m1;
    public GameObject SteamVR_Activator, left_Hand_Target, right_Hand_Target, left_Foot_Target, right_Foot_Target, pelvis_Target, head_Target;
    private bool configuration;

    // Use this for initialization
    void Start () {
        leftHand = GameObject.Find(this.gameObject.name + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
        Debug.Log("left Hand = " + leftHand.name);
        rightHand = GameObject.Find(this.gameObject.name + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/RightShoulder/RightArm/RightForeArm/RightHand");
        leftLeg = GameObject.Find(this.gameObject.name + "/CMU compliant skeleton/Hips/LHipJoint/LeftUpLeg/LeftLeg/LeftFoot");
        rightLeg = GameObject.Find(this.gameObject.name + "/CMU compliant skeleton/Hips/RHipJoint/RightUpLeg/RightLeg/RightFoot");
        pelvis = GameObject.Find(this.gameObject.name + "/CMU compliant skeleton/Hips");
        head = GameObject.Find(this.gameObject.name + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/Neck/Neck1");

        Debug.Log("left Hand Default Local Rotation = " + leftHand.transform.localEulerAngles);
        leftHand_Default_Rotation = leftHand.transform.localRotation;
        rightHand_Default_Rotation = rightHand.transform.localRotation;
        left_Foot_Default_Rotation = leftLeg.transform.localRotation;
        right_Foot_Default_Rotation = rightLeg.transform.localRotation;
        pelvis_Default_Rotation = pelvis.transform.localRotation;
        Debug.Log("Pelvis rotation = " + pelvis_Default_Rotation.eulerAngles);
        head_Default_Rotation = head.transform.localRotation;

        SteamVR_Activator = GameObject.Find("Steam_VR_Activator_&_Avatar_Handler");
        m1 = SteamVR_Activator.GetComponent<model_and_Steam_VR_Controller>();

        configuration = false;
    }
	
	// Update is called once per frame
	void Update () {
        if (head_Target == null)
            head_Target = GameObject.Find("[CameraRig]/Camera (eye)/Head Target");
        
        if (pelvis_Target == null)
            pelvis_Target = GameObject.Find("Other Targets/Pelvis_Bone_Tracker/Pelvis Target");

        if (left_Hand_Target == null)
            left_Hand_Target = GameObject.Find("Other Targets/Left_Hand_Tracker/Left Hand Target");

        if (right_Hand_Target == null)
            right_Hand_Target = GameObject.Find("Other Targets/Right_Hand_Tracker/Right Hand Target");

        if (left_Foot_Target == null)
            left_Foot_Target = GameObject.Find("Other Targets/Left_Foot_Tracker/Left Foot Target");

        if (right_Foot_Target == null)
            right_Foot_Target = GameObject.Find("Other Targets/Right_Foot_Tracker/Right Foot Target");

        if (head_Target != null && pelvis_Target != null && left_Hand_Target != null && right_Hand_Target != null && left_Foot_Target != null && right_Foot_Target != null && configuration == false)
        {
            leftHand_Updated_Rotation = leftHand.transform.localRotation;
            Debug.Log("left Hand Updated Local Rotation = " + leftHand.transform.localEulerAngles);
            Debug.Log("leftHandTracker Local Updated = " + left_Hand_Target.transform.localEulerAngles);
            Quaternion a = leftHand_Default_Rotation * Quaternion.Inverse(leftHand_Updated_Rotation);
            Quaternion b = left_Hand_Target.transform.localRotation;
            b = b * a;
            Debug.Log("Updated Quaternion in Euler Angles = " + b.eulerAngles);
            left_Hand_Target.transform.localRotation = b;

            rightHand_Updated_Rotation = rightHand.transform.localRotation;
            Quaternion c = rightHand_Default_Rotation * Quaternion.Inverse(rightHand_Updated_Rotation);
            Quaternion d = right_Hand_Target.transform.localRotation;
            d = d * c;
            right_Hand_Target.transform.localRotation = d;

            left_Foot_Updated_Rotation = leftLeg.transform.localRotation;
            Quaternion e = left_Foot_Default_Rotation * Quaternion.Inverse(left_Foot_Updated_Rotation);
            Quaternion f = left_Foot_Target.transform.localRotation;
            f = f * e;
            f *= Quaternion.Euler(0, 0, -90);
            left_Foot_Target.transform.localRotation = f;

            right_Foot_Updated_Rotation = rightLeg.transform.localRotation;
            Quaternion g = right_Foot_Default_Rotation * Quaternion.Inverse(right_Foot_Updated_Rotation);
            Quaternion h = right_Foot_Target.transform.localRotation;
            h = h * g;
            h *= Quaternion.Euler(0, 0, 90);
            right_Foot_Target.transform.localRotation = h;

            pelvis_Updated_Rotation = pelvis.transform.localRotation;
            Quaternion i = pelvis_Default_Rotation * Quaternion.Inverse(pelvis_Updated_Rotation);
            Quaternion j = pelvis_Target.transform.localRotation;
            j = j * i;
            pelvis_Target.transform.localRotation = j;

            head_Updated_Rotation = head.transform.localRotation;
            Quaternion k = head_Default_Rotation * Quaternion.Inverse(head_Updated_Rotation);
            Quaternion l = head_Target.transform.localRotation;
            l = l * k;
            head_Target.transform.localRotation = l;

            head_Target.transform.localPosition = new Vector3(0, 0.01f, -0.16f);
            pelvis_Target.transform.localPosition = new Vector3(0, -0.14f, -0.14f);
            right_Foot_Target.transform.localPosition = new Vector3(0, 0, -0.05f);
            left_Foot_Target.transform.localPosition = new Vector3(0, 0, -0.05f);

            right_Foot_Target.transform.localRotation = Quaternion.Euler(new Vector3(18, -170, -25));
            left_Foot_Target.transform.localRotation = Quaternion.Euler(new Vector3(-10, 165, -45));
            configuration = true;
        }
    }
}
