using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;
using RootMotion.Demos;

public class Tracker_Automatic_Calibrator : MonoBehaviour {
	public GameObject leftHand, rightHand, head;
	private Quaternion leftHand_Default_Rotation, rightHand_Default_Rotation, head_Default_Rotation;
	private Quaternion leftHand_Updated_Rotation, rightHand_Updated_Rotation, head_Updated_Rotation;
	public GameObject left_Hand_Target, right_Hand_Target, head_Target, tracker_location_controller;
	[HideInInspector]
	public bool configuration_done;
	private Quaternion calculated_difference_of_leftHand, calculated_difference_of_rightHand, calculated_difference_of_head;
	private TrackerCalibrationController tcc;


	// Use this for initialization
	void Start () {
		leftHand = GameObject.Find(this.gameObject.name + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
		rightHand = GameObject.Find(this.gameObject.name + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/RightShoulder/RightArm/RightForeArm/RightHand");
		head = GameObject.Find(this.gameObject.name + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/Neck/Neck1");
		left_Hand_Target = GameObject.Find("Other Targets/Left Hand Tracker/Left Hand Target");
		right_Hand_Target = GameObject.Find("Other Targets/Right Hand Tracker/Right Hand Target");
		head_Target = GameObject.Find("Other Targets/Head Tracker/Head Target");
		tracker_location_controller = GameObject.Find("Tracker_Location_Controller");

		leftHand_Default_Rotation = leftHand.transform.localRotation;
		rightHand_Default_Rotation = rightHand.transform.localRotation;
		head_Default_Rotation = head.transform.localRotation;

		configuration_done = false;
		// run the TrackerCalibrationController script
		tcc = tracker_location_controller.GetComponent<TrackerCalibrationController>();
	}

	// Update is called once per frame
	void Update () {
		leftHand_Updated_Rotation = leftHand.transform.localRotation;
		rightHand_Updated_Rotation = rightHand.transform.localRotation;
		head_Updated_Rotation = head.transform.localRotation;

		if (leftHand_Updated_Rotation != leftHand_Default_Rotation && rightHand_Updated_Rotation != rightHand_Default_Rotation && head_Updated_Rotation != head_Default_Rotation && configuration_done == false && tcc.tracker_calibration == true)
		{
			// calculate difference between default orientation and the tracker orientation
			Debug.Log("head Updated Local Rotation = " + head.transform.localEulerAngles);
			calculated_difference_of_head = head_Default_Rotation * Quaternion.Inverse(head_Updated_Rotation);
			Debug.Log("calculated_difference_of_head = " + calculated_difference_of_head.eulerAngles);

			Debug.Log("left Hand Updated Local Rotation = " + leftHand.transform.localEulerAngles);
			calculated_difference_of_leftHand = leftHand_Default_Rotation * Quaternion.Inverse(leftHand_Updated_Rotation);
			Debug.Log("calculated_difference_of_leftHand = " + calculated_difference_of_leftHand.eulerAngles);

			Debug.Log("right Hand Updated Local Rotation = " + rightHand.transform.localEulerAngles);
			calculated_difference_of_rightHand = rightHand_Default_Rotation * Quaternion.Inverse(rightHand_Updated_Rotation);
			Debug.Log("calculated_difference_of_rightHand = " + calculated_difference_of_rightHand.eulerAngles);

			Quaternion f = head_Target.transform.localRotation;
			Debug.Log("Quaternion f = " + f.eulerAngles);
			f *= calculated_difference_of_head;
			Debug.Log("Updated head_target in Euler Angles = " + f.eulerAngles);
			head_Target.transform.localRotation = f;
			Debug.Log("New head_target in Euler Angles = " + head_Target.transform.localEulerAngles);

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

			Quaternion head_Target_Offset_z = head_Target.transform.localRotation * Quaternion.Euler(0, 0, -90);
			Quaternion head_Target_Offset_x = head_Target_Offset_z * Quaternion.Euler(40, 0, 0);
			Quaternion head_Target_Offset_zxz = head_Target_Offset_x * Quaternion.Euler(0, 0, -30);
            Quaternion head_Target_Offset_zxzy = head_Target_Offset_zxz * Quaternion.Euler(0, 5, 0);
            head_Target.transform.localRotation = head_Target_Offset_zxzy;

            Quaternion rightHand_Target_Offset_x = right_Hand_Target.transform.localRotation * Quaternion.Euler(30, 0, 0);
            right_Hand_Target.transform.localRotation = rightHand_Target_Offset_x;

            Quaternion leftHand_Target_Offset_x = left_Hand_Target.transform.localRotation * Quaternion.Euler(90, 0, 0);
            Quaternion leftHand_Target_Offset_y = leftHand_Target_Offset_x * Quaternion.Euler(0, 180, 0);
            Quaternion leftHand_Target_Offset_xyx = leftHand_Target_Offset_y * Quaternion.Euler(30, 0, 0);
            left_Hand_Target.transform.localRotation = leftHand_Target_Offset_xyx/*zx*/;

            //Quaternion rightHand_Target_Offset_z = right_Hand_Target.transform.localRotation * Quaternion.Euler(0, 0, 180);
            //Quaternion rightHand_Target_Offset_x = rightHand_Target_Offset_z * Quaternion.Euler(40, 0, 0);
            //right_Hand_Target.transform.localRotation = rightHand_Target_Offset_x;

            //Quaternion leftHand_Target_Offset_x = left_Hand_Target.transform.localRotation * Quaternion.Euler(-40, 0, 0);
            //left_Hand_Target.transform.localRotation = leftHand_Target_Offset_x;

            configuration_done = true;
		}
	}
}
