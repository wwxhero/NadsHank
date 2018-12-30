using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Calibration_Script : MonoBehaviour
{
    private int count;
    private TrackerCalibrationController tcc;
    public GameObject modelUpperArmLeft, modelLowerArmLeft, modelLeftWristJoint;
    public GameObject modelLowerArmRight, modelUpperArmRight, modelRightWristJoint;
    public GameObject modelUpperLegLeft, modelUpperLegRight, modelLowerLegLeft, modelLowerLegRight, modelLeftFootJoint, modelRightFootJoint;
    private GameObject modelHead, modelPelvis, modelLeftWrist, modelRightWrist, modelLeftFoot, modelRightFoot;
    public GameObject modelActualHead, modelActualPelvis;
    private float leftHandLength, rightHandLength, leftFootLength, rightFootLength, headToPelvisLength, headToUpperArmLeftLength, headToUpperArmRightLength, pelvisToUpperLegLeftLength, pelvisToUpperLegRightLength;
    private float actualLeftHandLength, actualRightHandLength, actualLeftFootLength, actualRightFootLength, actualHeadToPelvisLength;
    public GameObject pelvis, head;
    private Vector3 headActualPosition, pelvisActualPosition, leftFootPosition, rightFootPosition;
    private float y_value, z_value;
    void Start()
    {
        Debug.Log("ped = " + gameObject.name);
        Debug.Log(gameObject.name + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/LeftShoulder/LeftArm");

        modelUpperArmLeft = GameObject.Find(gameObject.name + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/LeftShoulder/LeftArm");
        modelLowerArmLeft = GameObject.Find(gameObject.name + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/LeftShoulder/LeftArm/LeftForeArm");
        modelLeftWristJoint = GameObject.Find(gameObject.name + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
        modelLowerArmRight = GameObject.Find(gameObject.name + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/RightShoulder/RightArm/RightForeArm");
        modelUpperArmRight = GameObject.Find(gameObject.name + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/RightShoulder/RightArm");
        modelRightWristJoint = GameObject.Find(gameObject.name + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/RightShoulder/RightArm/RightForeArm/RightHand");
        modelUpperLegLeft = GameObject.Find(gameObject.name + "/CMU compliant skeleton/Hips/LHipJoint/LeftUpLeg");
        modelUpperLegRight = GameObject.Find(gameObject.name + "/CMU compliant skeleton/Hips/RHipJoint/RightUpLeg");
        modelLowerLegLeft = GameObject.Find(gameObject.name + "/CMU compliant skeleton/Hips/LHipJoint/LeftUpLeg/LeftLeg");
        modelLowerLegRight = GameObject.Find(gameObject.name + "/CMU compliant skeleton/Hips/RHipJoint/RightUpLeg/RightLeg");
        modelLeftFootJoint = GameObject.Find(gameObject.name + "/CMU compliant skeleton/Hips/LHipJoint/LeftUpLeg/LeftLeg/LeftFoot");
        modelRightFootJoint = GameObject.Find(gameObject.name + "/CMU compliant skeleton/Hips/RHipJoint/RightUpLeg/RightLeg/RightFoot");
        modelActualHead = GameObject.Find(gameObject.name + "/CMU compliant skeleton/Hips/LowerBack/Spine/Spine1/Neck/Neck1");
        modelActualPelvis = GameObject.Find(gameObject.name + "/CMU compliant skeleton/Hips");
        pelvis = GameObject.Find("Other Targets/Pelvis_Bone_Tracker/Pelvis");
        head = GameObject.Find("[CameraRig]/Camera (eye)/Head");

        count = 1;
        leftHandLength = Mathf.Round((Vector3.Distance(modelUpperArmLeft.transform.position, modelLowerArmLeft.transform.position) + Vector3.Distance(modelLowerArmLeft.transform.position, modelLeftWristJoint.transform.position)) * 100f) / 100f;
        rightHandLength = Mathf.Round((Vector3.Distance(modelUpperArmRight.transform.position, modelLowerArmRight.transform.position) + Vector3.Distance(modelLowerArmRight.transform.position, modelRightWristJoint.transform.position)) * 100f) / 100f;
        leftFootLength = Mathf.Round((Vector3.Distance(modelUpperLegLeft.transform.position, modelLowerLegLeft.transform.position) + Vector3.Distance(modelLowerLegLeft.transform.position, modelLeftFootJoint.transform.position)) * 100f) / 100f;
        rightFootLength = Mathf.Round((Vector3.Distance(modelUpperLegRight.transform.position, modelLowerLegRight.transform.position) + Vector3.Distance(modelLowerLegRight.transform.position, modelRightFootJoint.transform.position)) * 100f) / 100f;
        headToPelvisLength = Mathf.Round((Vector3.Distance(modelActualHead.transform.position, modelActualPelvis.transform.position)) * 100f) / 100f;
        headToUpperArmLeftLength = Mathf.Round(Vector3.Distance(modelActualHead.transform.position, modelUpperArmLeft.transform.position) * 100f) / 100f;
        headToUpperArmRightLength = Mathf.Round(Vector3.Distance(modelActualHead.transform.position, modelUpperArmRight.transform.position) * 100f) / 100f;
        pelvisToUpperLegLeftLength = Mathf.Round((modelActualPelvis.transform.position.y - modelUpperLegLeft.transform.position.y) * 100f) / 100f;
        pelvisToUpperLegRightLength = Mathf.Round((modelActualPelvis.transform.position.y - modelUpperLegRight.transform.position.y) * 100f) / 100f;

        Debug.Log("Left Hand Length = " + leftHandLength);
        Debug.Log("Left Foot Length = " + leftFootLength);
        Debug.Log("Right Hand Length = " + rightHandLength);
        Debug.Log("Right Foot Length = " + rightFootLength);
        Debug.Log("Head to Pelvis Length = " + headToPelvisLength);
        Debug.Log("Head to Left Upper Arm Length = " + headToUpperArmLeftLength);
        Debug.Log("Head to Right Upper Arm Length = " + headToUpperArmRightLength);
        Debug.Log("Pelvis to Left Upper Leg Length = " + pelvisToUpperLegLeftLength);
        Debug.Log("Pelvis to Right Upper Leg Length = " + pelvisToUpperLegRightLength);
        Debug.Log("Actual Pelvis position = " + modelActualPelvis.transform.position);
        
        Vector3 pelvisPosition = new Vector3(0f, 0.06f, -0.08f);
        Debug.Log("New Pelvis Position = " + pelvisPosition);
        pelvis.transform.localPosition = pelvisPosition;
    }

    // Update is called once per frame
    private void Update()
    {
        if (TrackerCalibrationController.count1 == true && this.count < 2)
        {
            if (modelHead == null)
                modelHead = GameObject.Find("Camera (eye)");
            if (modelPelvis == null)
                modelPelvis = GameObject.Find("Pelvis_Bone_Tracker");
            if (modelLeftWrist == null)
                modelLeftWrist = GameObject.Find("Left_Hand_Tracker");
            if (modelRightWrist == null)
                modelRightWrist = GameObject.Find("Right_Hand_Tracker");
            if (modelLeftFoot == null)
                modelLeftFoot = GameObject.Find("Left_Foot_Tracker");
            if (modelRightFoot == null)
                modelRightFoot = GameObject.Find("Right_Foot_Tracker");

            Debug.Log("Found GameObject = " + modelHead.name);
            Debug.Log("Found GameObject = " + modelPelvis.name);
            Debug.Log("Found GameObject = " + modelLeftWrist.name);
            Debug.Log("Found GameObject = " + modelLeftFoot.name);
            Debug.Log("Found GameObject = " + modelRightWrist.name);
            Debug.Log("Found GameObject = " + modelRightFoot.name);

            count++;
        }

        if (this.count == 2)
        {
            Debug.Log("Head Tracker Position before the if statement = " + modelHead.transform.position);
            Debug.Log("Pelvis Tracker Position before the if statement = " + modelPelvis.transform.position);
            Debug.Log("Left Wrist Tracker Position before the if statement = " + modelLeftWrist.transform.position);
            Debug.Log("Right Wrist Tracker Position before the if statement = " + modelRightWrist.transform.position);
            Debug.Log("Left Foot Tracker Position before the if statement = " + modelLeftFoot.transform.position);
            Debug.Log("Right Foot Tracker Position before the if statement = " + modelRightFoot.transform.position);

            Vector3 gameObject_positions = gameObject.transform.position /*new Vector3 (0f, 0f, 0f)*/;

            if (modelHead.transform.position != gameObject_positions && modelPelvis.transform.position != gameObject_positions && modelLeftWrist.transform.position != gameObject_positions
                && modelLeftFoot.transform.position != gameObject_positions && modelRightWrist.transform.position != gameObject_positions && modelRightFoot.transform.position != gameObject_positions)
            {
                Debug.Log("Head Tracker Position = " + modelHead.transform.position);
                Debug.Log("Pelvis Tracker Position = " + modelPelvis.transform.position);
                Debug.Log("Left Hand tracker Position = " + modelLeftWrist.transform.position);
                Debug.Log("Left Foot Tracker Position = " + modelLeftFoot.transform.position);
                Debug.Log("Right Hand Tracker Position = " + modelRightWrist.transform.position);
                Debug.Log("Right Foot Tracker Position = " + modelRightFoot.transform.position);

                actualHeadToPelvisLength = Mathf.Round(Vector3.Distance(modelHead.transform.position, modelPelvis.transform.position) * 100f) / 100f;
                actualLeftFootLength = Mathf.Round((Vector3.Distance(modelPelvis.transform.position, modelLeftFoot.transform.position) + pelvisToUpperLegLeftLength) * 100f) / 100f;
                actualRightFootLength = Mathf.Round((Vector3.Distance(modelPelvis.transform.position, modelRightFoot.transform.position) + pelvisToUpperLegRightLength) * 100f) / 100f;
                actualLeftHandLength = Mathf.Round((Vector3.Distance(modelHead.transform.position, modelLeftWrist.transform.position) - headToUpperArmLeftLength) * 100f) / 100f;
                actualRightHandLength = Mathf.Round((Vector3.Distance(modelHead.transform.position, modelRightWrist.transform.position) - headToUpperArmRightLength) * 100f) / 100f;
                
                float forTestingLeftHand = Vector3.Distance(modelHead.transform.position, modelLeftWrist.transform.position);
                Debug.Log("Testing Left Hand = " + forTestingLeftHand);

                float forTestingRightHand = Vector3.Distance(modelHead.transform.position, modelRightWrist.transform.position);
                Debug.Log("Testing Right Hand = " + forTestingRightHand);

                Debug.Log("Actual Left Hand Length = " + actualLeftHandLength);
                Debug.Log("Actual Right Hand Length = " + actualRightHandLength);
                Debug.Log("Actual Left Foot Length = " + actualLeftFootLength);
                Debug.Log("Actual Right Foot Length = " + actualRightFootLength);
                Debug.Log("Actual Head To Pelvis Length = " + actualHeadToPelvisLength);

                if (actualLeftHandLength > leftHandLength)
                {
                    float difference = Mathf.Round((actualLeftHandLength - leftHandLength) * 100f) / 100f;
                    Debug.Log("Left Hand Difference to decrease = " + difference);
                    float percentage = Mathf.Round(((difference / leftHandLength) * 100) * 100f) / 100f;
                    Debug.Log("Percentage = " + percentage);
                    float newScale1 = Mathf.Round((modelUpperArmLeft.transform.localScale.y + (modelUpperArmLeft.transform.localScale.y * (((percentage / 5) * 1.13f) / 100))) * 100f) / 100f;
                    float newScale2 = Mathf.Round((modelLowerArmLeft.transform.localScale.y + (modelLowerArmLeft.transform.localScale.y * (((percentage / 5) / 100)))) * 100f) / 100f;
                    
                    modelUpperArmLeft.transform.localScale = new Vector3(1f, newScale1, 1f);
                    modelLowerArmLeft.transform.localScale = new Vector3(1f, newScale2, 1f);
                    Debug.Log("Modified Left Upper Arm Size = " + modelUpperArmLeft.transform.localScale);
                    Debug.Log("Modified Left Lower Arm Size = " + modelLowerArmLeft.transform.localScale);
                }

                if (actualLeftHandLength < leftHandLength)
                {
                    float difference = Mathf.Round((leftHandLength - actualLeftHandLength) * 100f) / 100f;
                    Debug.Log("Left Hand Difference to increase = " + difference);
                    float percentage = Mathf.Round(((difference / leftHandLength) * 100) * 100f) / 100f;
                    Debug.Log("Percentage = " + percentage);
                    float newScale1 = Mathf.Round((modelUpperArmLeft.transform.localScale.y - (modelUpperArmLeft.transform.localScale.y * (((percentage / 5) * 1.13f) / 100))) * 100f) / 100f;
                    float newScale2 = Mathf.Round((modelLowerArmLeft.transform.localScale.y - (modelLowerArmLeft.transform.localScale.y * (((percentage / 5) / 100)))) * 100f) / 100f;
                    
                    modelUpperArmLeft.transform.localScale = new Vector3(1f, newScale1, 1f);
                    modelLowerArmLeft.transform.localScale = new Vector3(1f, newScale2, 1f);
                    Debug.Log("Modified Left Upper Arm Size = " + modelUpperArmLeft.transform.localScale);
                    Debug.Log("Modified Left Lower Arm Size = " + modelLowerArmLeft.transform.localScale);
                }

                if (actualRightHandLength > rightHandLength)
                {
                    float difference = Mathf.Round((actualRightHandLength - rightHandLength) * 100f) / 100f;
                    Debug.Log("Right Hand Difference to decrease = " + difference);
                    float percentage = Mathf.Round(((difference / rightHandLength) * 100) * 100f) / 100f;
                    Debug.Log("Percentage = " + percentage);
                    float newScale1 = Mathf.Round((modelUpperArmRight.transform.localScale.y + (modelUpperArmRight.transform.localScale.y * (((percentage / 5) * 1.13f) / 100))) * 100f) / 100f;
                    float newScale2 = Mathf.Round((modelLowerArmRight.transform.localScale.y + (modelLowerArmRight.transform.localScale.y * (((percentage / 5) / 100)))) * 100f) / 100f;
                    
                    modelUpperArmRight.transform.localScale = new Vector3(1f, newScale1, 1f);
                    modelLowerArmRight.transform.localScale = new Vector3(1f, newScale2, 1f);
                    Debug.Log("Modified Right Upper Arm Size = " + modelUpperArmRight.transform.localScale);
                    Debug.Log("Modified Right Lower Arm Size = " + modelLowerArmRight.transform.localScale);
                }

                if (actualRightHandLength < rightHandLength)
                {
                    float difference = Mathf.Round((rightHandLength - actualRightHandLength) * 100f) / 100f;
                    Debug.Log("Right Hand Difference to increase = " + difference);
                    float percentage = Mathf.Round(((difference / rightHandLength) * 100) * 100f) / 100f;
                    Debug.Log("Percentage = " + percentage);
                    float newScale1 = Mathf.Round((modelUpperArmRight.transform.localScale.y - (modelUpperArmRight.transform.localScale.y * (((percentage / 5) * 1.13f) / 100))) * 100f) / 100f;
                    float newScale2 = Mathf.Round((modelLowerArmRight.transform.localScale.y - (modelLowerArmRight.transform.localScale.y * (((percentage / 5) / 100)))) * 100f) / 100f;
                    
                    modelUpperArmRight.transform.localScale = new Vector3(1f, newScale1, 1f);
                    modelLowerArmRight.transform.localScale = new Vector3(1f, newScale2, 1f);
                    Debug.Log("Modified Right Upper Arm Size = " + modelUpperArmRight.transform.localScale);
                    Debug.Log("Modified Right Lower Arm Size = " + modelLowerArmRight.transform.localScale);
                }

                if (actualHeadToPelvisLength > headToPelvisLength)
                {
                    float difference = Mathf.Round((actualHeadToPelvisLength - headToPelvisLength) * 100f) / 100f;
                    Debug.Log("Head to Pelvis Difference to increase = " + difference);
                    float percentage = Mathf.Round(((difference / headToPelvisLength) * 100) * 100f) / 100f;
                    Debug.Log("Percentage = " + percentage);
                    float newScale = Mathf.Round((modelActualPelvis.transform.localScale.y + (modelActualPelvis.transform.localScale.y * ((percentage / 9) / 100))) * 100f) / 100f;
                    modelActualPelvis.transform.localScale = new Vector3(newScale, newScale, newScale);
                    Debug.Log("Modified Head to Pelvis Size = " + modelActualPelvis.transform.localScale);
                }

                if (actualHeadToPelvisLength < headToPelvisLength)
                {
                    float difference = Mathf.Round((headToPelvisLength - actualHeadToPelvisLength) * 100f) / 100f;
                    Debug.Log("Head to Pelvis Difference to decrease = " + difference);
                    float percentage = Mathf.Round(((difference / headToPelvisLength) * 100) * 100f) / 100f;
                    Debug.Log("Percentage = " + percentage);
                    float newScale = Mathf.Round((modelActualPelvis.transform.localScale.y - (modelActualPelvis.transform.localScale.y * ((percentage / 9) / 100))) * 100f) / 100f;
                    modelActualPelvis.transform.localScale = new Vector3(newScale, newScale, newScale);
                    Debug.Log("Modified Head to Pelvis Size = " + modelActualPelvis.transform.localScale);
                }

                if (actualLeftFootLength > leftFootLength)
                {
                    float difference = Mathf.Round((actualLeftFootLength - leftFootLength) * 100f) / 100f;
                    Debug.Log("Left Leg Difference to decrease = " + difference);
                    float percentage = Mathf.Round(((difference / leftFootLength) * 100) * 100f) / 100f;
                    Debug.Log("Percentage = " + percentage);
                    float newScale1 = Mathf.Round((modelUpperLegLeft.transform.localScale.y + (modelUpperLegLeft.transform.localScale.y * (((percentage / 5) * 1.13f) / 100))) * 100f) / 100f;
                    float newScale2 = Mathf.Round((modelLowerLegLeft.transform.localScale.y + (modelLowerLegLeft.transform.localScale.y * (((percentage / 5) / 100)))) * 100f) / 100f;
                    
                    modelUpperLegLeft.transform.localScale = new Vector3(1f, newScale1, 1f);
                    modelLowerLegLeft.transform.localScale = new Vector3(1f, newScale2, 1f);
                    Debug.Log("Modified Left Upper Leg Size = " + modelUpperLegLeft.transform.localScale);
                    Debug.Log("Modeified Left Lower Leg Size = " + modelLowerLegLeft.transform.localScale);
                }

                if (actualLeftFootLength < leftFootLength)
                {
                    float difference = Mathf.Round((leftFootLength - actualLeftFootLength) * 100f) / 100f;
                    Debug.Log("Left Leg Difference to increase = " + difference);
                    float percentage = Mathf.Round(((difference / leftFootLength) * 100) * 100f) / 100f;
                    Debug.Log("Percentage = " + percentage);
                    float newScale1 = Mathf.Round((modelUpperLegLeft.transform.localScale.y - (modelUpperLegLeft.transform.localScale.y * (((percentage / 5) * 1.13f) / 100))) * 100f) / 100f;
                    float newScale2 = Mathf.Round((modelLowerLegLeft.transform.localScale.y - (modelLowerLegLeft.transform.localScale.y * (((percentage / 5) / 100)))) * 100f) / 100f;
                    
                    modelUpperLegLeft.transform.localScale = new Vector3(1f, newScale1, 1f);
                    modelLowerLegLeft.transform.localScale = new Vector3(1f, newScale2, 1f);
                    Debug.Log("Modified Left Upper Leg Size = " + modelUpperLegLeft.transform.localScale);
                    Debug.Log("Modeified Left Lower Leg Size = " + modelLowerLegLeft.transform.localScale);
                }

                if (actualRightFootLength > rightFootLength)
                {
                    float difference = Mathf.Round((actualRightFootLength - rightFootLength) * 100f) / 100f;
                    Debug.Log("Right Leg Difference to decrease = " + difference);
                    float percentage = Mathf.Round(((difference / rightFootLength) * 100) * 100f) / 100f;
                    Debug.Log("Percentage = " + percentage);
                    float newScale1 = Mathf.Round((modelUpperLegRight.transform.localScale.y + (modelUpperLegRight.transform.localScale.y * (((percentage / 5) * 1.13f) / 100))) * 100f) / 100f;
                    float newScale2 = Mathf.Round((modelLowerLegRight.transform.localScale.y + (modelLowerLegRight.transform.localScale.y * (((percentage / 5) / 100)))) * 100f) / 100f;
                    
                    modelUpperLegRight.transform.localScale = new Vector3(1f, newScale1, 1f);
                    modelLowerLegRight.transform.localScale = new Vector3(1f, newScale2, 1f);
                    Debug.Log("Modified Right Upper Leg Size = " + modelUpperLegRight.transform.localScale);
                    Debug.Log("Modified Right Lower Leg Size = " + modelLowerLegRight.transform.localScale);
                }

                if (actualRightFootLength < rightFootLength)
                {
                    float difference = Mathf.Round((rightFootLength - actualRightFootLength) * 100f) / 100f;
                    Debug.Log("Right Leg Difference to increase = " + difference);
                    float percentage = Mathf.Round(((difference / rightFootLength) * 100) * 100f) / 100f;
                    Debug.Log("Percentage = " + percentage);
                    float newScale1 = Mathf.Round((modelUpperLegRight.transform.localScale.y - (modelUpperLegRight.transform.localScale.y * (((percentage / 5) * 1.13f) / 100))) * 100f) / 100f;
                    float newScale2 = Mathf.Round((modelLowerLegRight.transform.localScale.y - (modelLowerLegRight.transform.localScale.y * (((percentage / 5) / 100)))) * 100f) / 100f;
                    
                    modelUpperLegRight.transform.localScale = new Vector3(1f, newScale1, 1f);
                    modelLowerLegRight.transform.localScale = new Vector3(1f, newScale2, 1f);
                    Debug.Log("Modified Right Upper Leg Size = " + modelUpperLegRight.transform.localScale);
                    Debug.Log("Modified Right Lower Leg Size = " + modelLowerLegRight.transform.localScale);
                }
                count++;
            }
        }
    }
}