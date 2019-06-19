using UnityEngine;
using System.Collections;

namespace RootMotion.FinalIK
{

	/// <summary>
	/// Calibrates VRIK for the HMD and up to 5 additional trackers.
	/// </summary>
	public static class VRIKCalibrator2
	{
		static readonly string c_targetName = "target";
		static readonly string c_goalName = "goal";
		public static void UnCalibrate(VRIK ik, Transform headTracker, Transform bodyTracker, Transform leftHandTracker, Transform rightHandTracker, Transform leftFootTracker, Transform rightFootTracker)
		{
			VRIKBackup bk = ik.GetComponent<VRIKBackup>();
			Debug.Assert(null != bk);
			bk.Restore(ik);
			Transform [] trackers = {
									  headTracker
									, bodyTracker
									, leftHandTracker
									, rightHandTracker
									, leftFootTracker
									, rightFootTracker
								};
			int n_tracker = trackers.Length;
			for (int i_tracker = 0; i_tracker < n_tracker; i_tracker ++)
			{
				Transform tracker = trackers[i_tracker];
				foreach (Transform child in tracker)
					if (child.name == c_targetName
						|| child.name == c_goalName)
					GameObject.Destroy(child.gameObject);
			}
		}

		public static VRIKCalibrator.CalibrationData Calibrate(VRIK ik, Transform headTracker, Transform bodyTracker, Transform leftHandTracker, Transform rightHandTracker, Transform leftFootTracker, Transform rightFootTracker)
		{
			VRIKBackup bk = ik.GetComponent<VRIKBackup>();
			Debug.Assert(null != bk);
			bk.Save(ik);
			if (!ik.solver.initiated)
			{
				Debug.LogError("Can not calibrate before VRIK has initiated.");
				return null;
			}
			//ik.solver.FixTransforms();
			//enum Parts{ head = 0, pelvis = 1, lhand = 2, rhand = 3, lfoot = 4, rfoot = 5 };
			Transform [] trackers = {
									  headTracker
									, bodyTracker
									, leftHandTracker
									, rightHandTracker
									, leftFootTracker
									, rightFootTracker
								};

			Transform [] refs_target = {
									  ik.references.head
									, ik.references.pelvis
									, ik.references.leftHand
									, ik.references.rightHand
									, ik.references.leftToes
									, ik.references.rightToes
								};
			Transform [] ref_goals = {
									  null
									, null
									, null
									, null
									, ik.references.leftFoot
									, ik.references.rightFoot
								};
			int n_tracker = trackers.Length;
			GameObject[] targets = new GameObject[n_tracker];
			GameObject[] goals = new GameObject[n_tracker];

			for (int i_tracker = 0; i_tracker < n_tracker; i_tracker ++)
			{
				GameObject target = new GameObject(c_targetName);
				target.transform.rotation = refs_target[i_tracker].rotation;
				target.transform.position = refs_target[i_tracker].position;
				target.transform.parent = trackers[i_tracker];
				targets[i_tracker] = target;
				if (null == ref_goals[i_tracker])
					continue;
				GameObject goal = new GameObject(c_goalName);
				goal.transform.position = ref_goals[i_tracker].position + ik.references.root.transform.forward + ik.references.root.transform.up;
				goal.transform.parent = trackers[i_tracker];
				goals[i_tracker] = goal;
			}

			VRIKCalibrator.CalibrationData data = new VRIKCalibrator.CalibrationData();
			data.scale = 1;
			data.pelvisRotationWeight = 1;
			data.pelvisPositionWeight = 1;
			data.head = new VRIKCalibrator.CalibrationData.Target(targets[0].transform);
			data.pelvis = new VRIKCalibrator.CalibrationData.Target(targets[1].transform);
			data.leftHand = new VRIKCalibrator.CalibrationData.Target(targets[2].transform);
			data.rightHand = new VRIKCalibrator.CalibrationData.Target(targets[3].transform);
			data.leftFoot = new VRIKCalibrator.CalibrationData.Target(targets[4].transform);
			data.rightFoot = new VRIKCalibrator.CalibrationData.Target(targets[5].transform);
			data.leftLegGoal = new VRIKCalibrator.CalibrationData.Target(goals[4].transform);
			data.rightLegGoal = new VRIKCalibrator.CalibrationData.Target(goals[5].transform);


			ik.solver.spine.headTarget = targets[0].transform;
			ik.solver.spine.pelvisTarget = targets[1].transform;
			ik.solver.spine.pelvisPositionWeight = data.pelvisPositionWeight;
			ik.solver.spine.pelvisRotationWeight = data.pelvisRotationWeight;
			ik.solver.plantFeet = false;
			ik.solver.spine.maxRootAngle = 180f;
			ik.solver.leftArm.target = targets[2].transform;
			ik.solver.leftArm.positionWeight = 1f;
			ik.solver.leftArm.rotationWeight = 1f;
			ik.solver.rightArm.target = targets[3].transform;
			ik.solver.rightArm.positionWeight = 1f;
			ik.solver.rightArm.rotationWeight = 1f;
			ik.solver.leftLeg.target = targets[4].transform;
			ik.solver.leftLeg.positionWeight = 1f;
			ik.solver.leftLeg.rotationWeight = 1f;
			ik.solver.leftLeg.bendGoal = goals[4].transform;
			ik.solver.leftLeg.bendGoalWeight = 1f;
			ik.solver.rightLeg.target = targets[5].transform;
			ik.solver.rightLeg.positionWeight = 1f;
			ik.solver.rightLeg.rotationWeight = 1f;
			ik.solver.rightLeg.bendGoal = goals[5].transform;
			ik.solver.rightLeg.bendGoalWeight = 1f;

			bool addRootController = bodyTracker != null || (leftFootTracker != null && rightFootTracker != null);
			var rootController = ik.references.root.GetComponent<VRIKRootController>();
			if (addRootController)
			{
				if (rootController == null) rootController = ik.references.root.gameObject.AddComponent<VRIKRootController>();
				rootController.Calibrate();
			}
			else
			{
				if (rootController != null) GameObject.Destroy(rootController);
			}
			data.pelvisTargetRight = rootController.pelvisTargetRight;

			ik.solver.spine.minHeadHeight = 0f;
			ik.solver.locomotion.weight = bodyTracker == null && leftFootTracker == null && rightFootTracker == null ? 1f : 0f;

			return data;
		}

		public static bool CalibrateStem(VRIK ik, Transform headTracker, Transform bodyTracker, Transform leftFootTracker, Transform rightFootTracker, VRIKCalibrator.CalibrationData data)
		{
			if (!ik.solver.initiated)
			{
				Debug.LogError("Can not calibrate before VRIK has initiated.");
				return false;
			}
			//ik.solver.FixTransforms();
			//enum Parts{ head = 0, pelvis = 1, lhand = 2, rhand = 3, lfoot = 4, rfoot = 5 };
			Transform [] trackers = {
									  headTracker
									, bodyTracker
									, leftFootTracker
									, rightFootTracker
								};

			//Debug.Assert(GameObject.FindGameObjectsWithTag("head_target")[0].transform == ik.references.head);
			Transform [] refs_target = {
									  ik.references.head
									, ik.references.pelvis
									, ik.references.leftToes
									, ik.references.rightToes
								};
			Transform [] ref_goals = {
									  null
									, null
									, ik.references.leftFoot
									, ik.references.rightFoot
								};
			int n_tracker = trackers.Length;
			GameObject[] targets = new GameObject[n_tracker];
			GameObject[] goals = new GameObject[n_tracker];

			for (int i_tracker = 0; i_tracker < n_tracker; i_tracker ++)
			{
				GameObject target = new GameObject("target");
				target.transform.rotation = refs_target[i_tracker].rotation;
				target.transform.position = refs_target[i_tracker].position;
				target.transform.parent = trackers[i_tracker];
				targets[i_tracker] = target;
				if (null == ref_goals[i_tracker])
					continue;
				GameObject goal = new GameObject("goal");
				goal.transform.position = ref_goals[i_tracker].position + ik.references.root.transform.forward + ik.references.root.transform.up;
				goal.transform.parent = trackers[i_tracker];
				goals[i_tracker] = goal;
			}

			data.scale = 1;
			data.pelvisRotationWeight = 1;
			data.pelvisPositionWeight = 1;
			data.head = new VRIKCalibrator.CalibrationData.Target(targets[0].transform);
			data.pelvis = new VRIKCalibrator.CalibrationData.Target(targets[1].transform);
			data.leftFoot = new VRIKCalibrator.CalibrationData.Target(targets[2].transform);
			data.rightFoot = new VRIKCalibrator.CalibrationData.Target(targets[3].transform);
			data.leftLegGoal = new VRIKCalibrator.CalibrationData.Target(goals[2].transform);
			data.rightLegGoal = new VRIKCalibrator.CalibrationData.Target(goals[3].transform);


			ik.solver.spine.headTarget = targets[0].transform;
			ik.solver.spine.pelvisTarget = targets[1].transform;
			ik.solver.spine.pelvisPositionWeight = data.pelvisPositionWeight;
			ik.solver.spine.pelvisRotationWeight = data.pelvisRotationWeight;
			ik.solver.plantFeet = false;
			ik.solver.spine.maxRootAngle = 180f;
			ik.solver.leftLeg.target = targets[2].transform;
			ik.solver.leftLeg.positionWeight = 1f;
			ik.solver.leftLeg.rotationWeight = 1f;
			ik.solver.leftLeg.bendGoal = goals[2].transform;
			ik.solver.leftLeg.bendGoalWeight = 1f;
			ik.solver.rightLeg.target = targets[3].transform;
			ik.solver.rightLeg.positionWeight = 1f;
			ik.solver.rightLeg.rotationWeight = 1f;
			ik.solver.rightLeg.bendGoal = goals[3].transform;
			ik.solver.rightLeg.bendGoalWeight = 1f;

			bool addRootController = bodyTracker != null || (leftFootTracker != null && rightFootTracker != null);
			var rootController = ik.references.root.GetComponent<VRIKRootController>();
			if (addRootController)
			{
				if (rootController == null) rootController = ik.references.root.gameObject.AddComponent<VRIKRootController>();
				rootController.Calibrate();
			}
			else
			{
				if (rootController != null) GameObject.Destroy(rootController);
			}
			data.pelvisTargetRight = rootController.pelvisTargetRight;

			ik.solver.spine.minHeadHeight = 0f;
			ik.solver.locomotion.weight = bodyTracker == null && leftFootTracker == null && rightFootTracker == null ? 1f : 0f;
			return true;
		}

		public static bool CalibrateLeftHand(VRIK ik, Transform leftHandTracker, VRIKCalibrator.CalibrationData data)
		{
			Transform ref_target = ik.references.leftHand;
			GameObject target = new GameObject("target");
			target.transform.rotation = ref_target.rotation;
			target.transform.position = ref_target.position;
			target.transform.parent = leftHandTracker;
			data.leftHand = new VRIKCalibrator.CalibrationData.Target(target.transform);
			ik.solver.leftArm.target = target.transform;
			ik.solver.leftArm.positionWeight = 1f;
			ik.solver.leftArm.rotationWeight = 1f;
			return true;
		}

		public static bool CalibrateRightHand(VRIK ik, Transform rightHandTracker, VRIKCalibrator.CalibrationData data)
		{
			Transform ref_target = ik.references.rightHand;
			GameObject target = new GameObject("target");
			target.transform.rotation = ref_target.rotation;
			target.transform.position = ref_target.position;
			target.transform.parent = rightHandTracker;
			data.rightHand = new VRIKCalibrator.CalibrationData.Target(target.transform);
			ik.solver.rightArm.target = target.transform;
			ik.solver.rightArm.positionWeight = 1f;
			ik.solver.rightArm.rotationWeight = 1f;
			return true;
		}

	}
}
