using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;
using RootMotion.Demos;

public class Tracker_Automatic_Calibrator : MonoBehaviour {
    public GameObject leftHand, rightHand, leftLeg, rightLeg, pelvis, head;
    private Quaternion leftHand_Default_Rotation, left_Foot_Default_Rotation, rightHand_Default_Rotation, right_Foot_Default_Rotation, pelvis_Default_Rotation, head_Default_Rotation;
    private Quaternion leftHand_Updated_Rotation, left_Foot_Updated_Rotation, rightHand_Updated_Rotation, right_Foot_Updated_Rotation, pelvis_Updated_Rotation, head_Updated_Rotation;
    public model_and_Steam_VR_Controller m1;
    public GameObject SteamVR_Activator, left_Hand_Target, right_Hand_Target, left_Foot_Target, right_Foot_Target, pelvis_Target, head_Target;
    //public GameObject makeHumanModel;
    private bool configuration/*, tracker_configuration*/;
    private Quaternion calculated_difference_of_leftHand, calculated_difference_of_rightHand, calculated_difference_of_leftFoot, calculated_difference_of_rightFoot, calculated_difference_of_pelvis, calculated_difference_of_head;
    private Vector3 default_head_position, default_pelvis_position;

    // Use this for initialization
    void Start () {
        //makeHumanModel = GameObject.Find("makeHuman31Bone");
        leftHand = GameObject.Find(this.gameObject.name/*makeHumanModel.name*/ + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
        //Debug.Log("left Hand = " + leftHand.name);
        rightHand = GameObject.Find(this.gameObject.name/*makeHumanModel.name*/ + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/RightShoulder/RightArm/RightForeArm/RightHand");
        leftLeg = GameObject.Find(this.gameObject.name/*makeHumanModel.name*/ + "/CMU compliant skeleton/Hips/LHipJoint/LeftUpLeg/LeftLeg/LeftFoot");
        rightLeg = GameObject.Find(this.gameObject.name/*makeHumanModel.name*/ + "/CMU compliant skeleton/Hips/RHipJoint/RightUpLeg/RightLeg/RightFoot");
        pelvis = GameObject.Find(this.gameObject.name/*makeHumanModel.name*/ + "/CMU compliant skeleton/Hips");
        head = GameObject.Find(this.gameObject.name/*makeHumanModel.name*/ + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/Neck/Neck1");
        
        //Debug.Log("left Hand Default Local Rotation = " + leftHand.transform.localEulerAngles);
        //Debug.Log("right Hand Default Local Rotation = " + rightHand.transform.localEulerAngles);
        //Debug.Log("left Foot Default Local Rotation = " + leftLeg.transform.localEulerAngles);
        //Debug.Log("right Foot Default Local Rotation = " + rightLeg.transform.localEulerAngles);
        //Debug.Log("pelvis Default Local Rotation = " + pelvis.transform.localEulerAngles);
        //Debug.Log("head Default Local Rotation = " + head.transform.localEulerAngles);

        leftHand_Default_Rotation = /*Quaternion.Euler(new Vector3(10.9f, 359.3f, 6.5f))*/ leftHand.transform.localRotation;
        rightHand_Default_Rotation = /*Quaternion.Euler(new Vector3(10.9f, 0.7f, 353.5f))*/ rightHand.transform.localRotation;
        left_Foot_Default_Rotation = Quaternion.Euler(new Vector3(287.2f, 0.0f, 4.1f)) /*leftLeg.transform.localRotation*/;
        right_Foot_Default_Rotation = Quaternion.Euler(new Vector3(287.2f, 0.0f, 355.9f)) /*rightLeg.transform.localRotation*/;
        pelvis_Default_Rotation = Quaternion.Euler(new Vector3(334.1f, 0.0f, 0.0f)) /*pelvis.transform.localRotation*/;
        head_Default_Rotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, 0.0f)) /*head.transform.localRotation*/;

        default_head_position = new Vector3(0.0f, 0.1f, 0.0f);
        default_pelvis_position = new Vector3(0.0f, 0.9f, 0.0f);

        Debug.Log("Head position = " + default_head_position);
        Debug.Log("Pelvis position = " + default_pelvis_position);

        SteamVR_Activator = GameObject.Find("Steam_VR_Activator_&_Avatar_Handler");
        m1 = SteamVR_Activator.GetComponent<model_and_Steam_VR_Controller>();

        configuration = false;
        //tracker_configuration = false;
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

        if (configuration == false)
        {
            left_Foot_Updated_Rotation = leftLeg.transform.localRotation;
            Debug.Log("left Foot Updated Local Rotation = " + leftLeg.transform.localEulerAngles);
            calculated_difference_of_leftFoot = left_Foot_Default_Rotation * Quaternion.Inverse(left_Foot_Updated_Rotation);
            Debug.Log("calculated_difference_of_leftFoot = " + calculated_difference_of_leftFoot.eulerAngles);

            right_Foot_Updated_Rotation = rightLeg.transform.localRotation;
            Debug.Log("right Foot Updated Local Rotation = " + rightLeg.transform.localEulerAngles);
            calculated_difference_of_rightFoot = right_Foot_Default_Rotation * Quaternion.Inverse(right_Foot_Updated_Rotation);
            Debug.Log("calculated_difference_of_rightFoot = " + calculated_difference_of_rightFoot.eulerAngles);

            pelvis_Updated_Rotation = pelvis.transform.localRotation;
            Debug.Log("pelvis Updated Local Rotation = " + pelvis.transform.localEulerAngles);
            calculated_difference_of_pelvis = pelvis_Default_Rotation * Quaternion.Inverse(pelvis_Updated_Rotation);
            Debug.Log("calculated_difference_of_pelvis = " + calculated_difference_of_pelvis.eulerAngles);

            

            configuration = true;
        }

        if (head_Target != null && pelvis_Target != null && left_Hand_Target != null && right_Hand_Target != null && left_Foot_Target != null && right_Foot_Target != null && m1.tracker_configuration == true)
        {
            leftHand_Updated_Rotation = leftHand.transform.localRotation;
            Debug.Log("left Hand Updated Local Rotation = " + leftHand.transform.localEulerAngles);
            //Debug.Log("leftHandTracker Local Updated = " + left_Hand_Target.transform.localEulerAngles);
            calculated_difference_of_leftHand = leftHand_Default_Rotation * Quaternion.Inverse(leftHand_Updated_Rotation);
            Debug.Log("calculated_difference_of_leftHand = " + calculated_difference_of_leftHand.eulerAngles);

            rightHand_Updated_Rotation = rightHand.transform.localRotation;
            Debug.Log("right Hand Updated Local Rotation = " + rightHand.transform.localEulerAngles);
            calculated_difference_of_rightHand = rightHand_Default_Rotation * Quaternion.Inverse(rightHand_Updated_Rotation);
            Debug.Log("calculated_difference_of_rightHand = " + calculated_difference_of_rightHand.eulerAngles);

            head_Updated_Rotation = head.transform.localRotation;
            Debug.Log("head Updated Local Rotation = " + head.transform.localEulerAngles);
            calculated_difference_of_head = head_Default_Rotation * Quaternion.Inverse(head_Updated_Rotation);
            Debug.Log("calculated_difference_of_head = " + calculated_difference_of_head.eulerAngles);

            Quaternion a = left_Hand_Target.transform.localRotation;
            Debug.Log("Quaternion a = " + a.eulerAngles);
            a *= calculated_difference_of_leftHand;
            Debug.Log("Updated left_hand_target in Euler Angles = " + a.eulerAngles);
            left_Hand_Target.transform.localRotation = a;
            Debug.Log("New left_hand_target in Euler Angles = " + left_Hand_Target.transform.localEulerAngles);

            Quaternion b = right_Hand_Target.transform.localRotation;
            Debug.Log("Quaternion b = " + b.eulerAngles);
            b *= calculated_difference_of_rightHand;
            Debug.Log("Updated right_hand_target in Euler Angles = " + b.eulerAngles);
            right_Hand_Target.transform.localRotation = b;
            Debug.Log("New right_hand_target in Euler Angles = " + right_Hand_Target.transform.localEulerAngles);

            Quaternion c = left_Foot_Target.transform.localRotation;
            Debug.Log("Quaternion c = " + c.eulerAngles);
            c *= calculated_difference_of_leftFoot;
            c *= Quaternion.Euler(0, 0, -90);
            Debug.Log("Updated left_foot_target in Euler Angles = " + c.eulerAngles);
            left_Foot_Target.transform.localRotation = c;
            Debug.Log("New left_Foot_target in Euler Angles = " + left_Foot_Target.transform.localEulerAngles);

            Quaternion d = right_Foot_Target.transform.localRotation;
            Debug.Log("Quaternion d = " + d.eulerAngles);
            d *= calculated_difference_of_rightFoot;
            d *= Quaternion.Euler(0, 0, 90);
            Debug.Log("Updated right_foot_target in Euler Angles = " + d.eulerAngles);
            right_Foot_Target.transform.localRotation = d;
            Debug.Log("New right_Foot_target in Euler Angles = " + right_Foot_Target.transform.localEulerAngles);

            Quaternion e = pelvis_Target.transform.localRotation;
            Debug.Log("Quaternion e = " + e.eulerAngles);
            e *= calculated_difference_of_pelvis;
            Debug.Log("Updated pelvis_target in Euler Angles = " + e.eulerAngles);
            pelvis_Target.transform.localRotation = e;
            Debug.Log("New pelvis_target in Euler Angles = " + pelvis_Target.transform.localEulerAngles);

            Quaternion f = head_Target.transform.localRotation;
            Debug.Log("Quaternion f = " + f.eulerAngles);
            f *= calculated_difference_of_head;
            Debug.Log("Updated head_target in Euler Angles = " + f.eulerAngles);
            head_Target.transform.localRotation = f;
            Debug.Log("New head_target in Euler Angles = " + head_Target.transform.localEulerAngles);

            Vector3 head_target_position = head_Target.transform.localPosition;
            Vector3 pelvis_target_position = pelvis_Target.transform.localPosition;
            Quaternion left_hand_target_rotation = left_Hand_Target.transform.localRotation;
            Quaternion right_hand_target_rotation = right_Hand_Target.transform.localRotation;
            Quaternion left_foot_target_rotation = left_Foot_Target.transform.localRotation;
            Quaternion right_foot_target_rotation = right_Foot_Target.transform.localRotation;

            Debug.Log("head_target_position = " + head_target_position);
            Debug.Log("pelvis_target_position = " + pelvis_target_position);

            //head_target_position.z += (-0.15f);
            //head_target_position.y += 0.17f;
            //head_Target.transform.localPosition = new Vector3 (head_target_position.x, head_target_position.y, head_target_position.z);

            //pelvis_target_position.z += 0.25f;
            //pelvis_Target.transform.localPosition = new Vector3(pelvis_target_position.x, pelvis_target_position.y, pelvis_target_position.z);

            //head_Target.transform.localPosition = default_head_position;
            //pelvis_Target.transform.localPosition = default_pelvis_position;

            left_hand_target_rotation *= Quaternion.Euler(new Vector3(20, 0, 0));
            left_Hand_Target.transform.localRotation = left_hand_target_rotation;

            right_hand_target_rotation *= Quaternion.Euler(new Vector3(20, 0, 0));
            right_Hand_Target.transform.localRotation = right_hand_target_rotation;

            left_foot_target_rotation *= Quaternion.Euler(new Vector3(0, -20, 0));
            left_Foot_Target.transform.localRotation = left_foot_target_rotation;

            right_foot_target_rotation *= Quaternion.Euler(new Vector3(0, 20, 0));
            right_Foot_Target.transform.localRotation = right_foot_target_rotation;

            Vector3 left_Foot_Position = left_Foot_Target.transform.localPosition;
            left_Foot_Position -= new Vector3(0.2f, 0.01f, 0.01f);
            left_Foot_Target.transform.localPosition = left_Foot_Position;

            Vector3 right_Foot_Position = right_Foot_Target.transform.localPosition;
            right_Foot_Position -= new Vector3(0.13f, -0.18f, 0.01f);
            right_Foot_Target.transform.localPosition = right_Foot_Position;

            //head_target_position.z += (-0.1f);
            //head_target_position.y += 0.06f;
            //pelvis_target_position.z -= 0.15f;

            //head_Target.transform.localPosition = new Vector3(0, 0.01f, -0.16f);
            //pelvis_Target.transform.localPosition = new Vector3(0, -0.14f, -0.14f);
            //right_Foot_Target.transform.localPosition = new Vector3(0, 0, -0.05f);
            //left_Foot_Target.transform.localPosition = new Vector3(0, 0, -0.05f);

            // right_Foot_Target.transform.localRotation = Quaternion.Euler(new Vector3(18, -170, -25));
            // left_Foot_Target.transform.localRotation = Quaternion.Euler(new Vector3(-10, 165, -45));
            m1.tracker_configuration = false;
        }
    }
}
