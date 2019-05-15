using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRIK_Targets_Assigner : MonoBehaviour {
    public GameObject headTarget, leftHandTarget, rightHandTarget, avatar, car, otherTargets;
    private Vector3 head_Position, head_Position_without_y, car_position_without_y;
    private int count;
    private Tracker_Automatic_Calibrator tac;

    // Use this for initialization
    void Start () {
        count = 1;
        tac = avatar.GetComponent<Tracker_Automatic_Calibrator>();
        RootMotion.FinalIK.VRIK ik = avatar.GetComponent<RootMotion.FinalIK.VRIK>();
        ik.solver.spine.headTarget = headTarget.transform;
        ik.solver.leftArm.target = leftHandTarget.transform;
        ik.solver.rightArm.target = rightHandTarget.transform;
	}

	// Update is called once per frame
	void Update () {
        if (count == 4 && tac.configuration_done == true)
        {
            head_Position = headTarget.transform.position;
            car_position_without_y = new Vector3(head_Position.x, 0, head_Position.z);
            head_Position_without_y = car_position_without_y;

            car.transform.position = car_position_without_y;
            otherTargets.transform.parent = car.transform;
            float offset = 0.5f;
            otherTargets.transform.localPosition += new Vector3(-offset, 0f, 0f) /*new Vector3(0f, -1.7f, 0.15f)*/;
            Vector3 head_Offset_z = head_Position_without_y + new Vector3(0f, 0f, offset);
            Vector3 head_Offset_y = head_Offset_z + new Vector3(0f, offset - 0.1f, 0f);
            Vector3 head_Offset_x = head_Offset_y + new Vector3(offset, 0f, 0f);
            avatar.transform.position = head_Offset_x;
            avatar.transform.parent = car.transform;
            avatar.transform.localRotation = Quaternion.Euler(90, 0, 0);
            count++;
        }

        if (count < 4)
            count++;
        else
            count = 5;
    }
}
