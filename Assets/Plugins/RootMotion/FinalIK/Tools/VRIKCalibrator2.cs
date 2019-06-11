using UnityEngine;
using System.Collections;

namespace RootMotion.FinalIK
{

	/// <summary>
	/// Calibrates VRIK for the HMD and up to 5 additional trackers.
	/// </summary>
	public static class VRIKCalibrator2
	{

		/// <summary>
		/// The settings for VRIK tracker calibration.
		/// </summary>
		[System.Serializable]
		public class Settings
		{

			/// <summary>
			/// Multiplies character scale.
			/// </summary>
			[Tooltip("Multiplies character scale")]
			public float scaleMlp = 1f;

			/// <summary>
			/// Local axis of the HMD facing forward.
			/// </summary>
			[Tooltip("Local axis of the HMD facing forward.")]
			public Vector3 headTrackerForward = Vector3.forward;

			/// <summary>
			/// Local axis of the HMD facing up.
			/// </summary>
			[Tooltip("Local axis of the HMD facing up.")]
			public Vector3 headTrackerUp = Vector3.up;

			/// <summary>
			/// Local axis of the body tracker towards the player's forward direction.
			/// </summary>
			[Tooltip("Local axis of the body tracker towards the player's forward direction.")]
			public Vector3 bodyTrackerForward = Vector3.forward;

			/// <summary>
			/// Local axis of the body tracker towards the up direction.
			/// </summary>
			[Tooltip("Local axis of the body tracker towards the up direction.")]
			public Vector3 bodyTrackerUp = Vector3.up;

			/// <summary>
			/// Local axis of the hand trackers pointing from the wrist towards the palm.
			/// </summary>
			[Tooltip("Local axis of the hand trackers pointing from the wrist towards the palm.")]
			public Vector3 handTrackerForward = Vector3.forward;

			/// <summary>
			/// Local axis of the hand trackers pointing in the direction of the surface normal of the back of the hand.
			/// </summary>
			[Tooltip("Local axis of the hand trackers pointing in the direction of the surface normal of the back of the hand.")]
			public Vector3 handTrackerUp = Vector3.up;

			/// <summary>
			/// Local axis of the foot trackers towards the player's forward direction.
			/// </summary>
			[Tooltip("Local axis of the foot trackers towards the player's forward direction.")]
			public Vector3 footTrackerForward = Vector3.forward;

			/// <summary>
			/// Local axis of the foot tracker towards the up direction.
			/// </summary>
			[Tooltip("Local axis of the foot tracker towards the up direction.")]
			public Vector3 footTrackerUp = Vector3.up;

			[Space(10f)]
			/// <summary>
			/// Offset of the head bone from the HMD in (headTrackerForward, headTrackerUp) space relative to the head tracker.
			/// </summary>
			[Tooltip("Offset of the head bone from the HMD in (headTrackerForward, headTrackerUp) space relative to the head tracker.")]
			public Vector3 headOffset;

			/// <summary>
			/// Offset of the hand bones from the hand trackers in (handTrackerForward, handTrackerUp) space relative to the hand trackers.
			/// </summary>
			[Tooltip("Offset of the hand bones from the hand trackers in (handTrackerForward, handTrackerUp) space relative to the hand trackers.")]
			public Vector3 handOffset;

			/// <summary>
			/// Forward offset of the foot bones from the foot trackers.
			/// </summary>
			[Tooltip("Forward offset of the foot bones from the foot trackers.")]
			public float footForwardOffset;

			/// <summary>
			/// Inward offset of the foot bones from the foot trackers.
			/// </summary>
			[Tooltip("Inward offset of the foot bones from the foot trackers.")]
			public float footInwardOffset;

			/// <summary>
			/// Used for adjusting foot heading relative to the foot trackers.
			/// </summary>
			[Tooltip("Used for adjusting foot heading relative to the foot trackers.")]
			[Range(-180f, 180f)]
			public float footHeadingOffset;

			/// <summary>
			/// Pelvis target position weight. If the body tracker is on the backpack or somewhere else not very close to the pelvis of the player, position weight needs to be reduced to allow some bending for the spine.
			/// </summary>
			[Range(0f, 1f)] public float pelvisPositionWeight = 1f;

			/// <summary>
			/// Pelvis target rotation weight. If the body tracker is on the backpack or somewhere else not very close to the pelvis of the player, rotation weight needs to be reduced to allow some bending for the spine.
			/// </summary>
			[Range(0f, 1f)] public float pelvisRotationWeight = 1f;
		}

		public static VRIKCalibrator.CalibrationData Calibrate(VRIK ik, Transform headTracker, Transform bodyTracker, Transform leftHandTracker, Transform rightHandTracker, Transform leftFootTracker, Transform rightFootTracker)
		{
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

			Debug.Assert(GameObject.FindGameObjectsWithTag("head_target")[0].transform == ik.references.head);
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
				GameObject target = new GameObject("target");
				target.transform.rotation = refs_target[i_tracker].rotation;
				target.transform.position = refs_target[i_tracker].position;
				target.transform.parent = trackers[i_tracker];
				targets[i_tracker] = target;
				if (null == ref_goals[i_tracker])
					continue;
				GameObject goal = new GameObject("goal");
				goal.transform.position = ref_goals[i_tracker].position + Vector3.forward + Vector3.up;
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

			Debug.Assert(GameObject.FindGameObjectsWithTag("head_target")[0].transform == ik.references.head);
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
				goal.transform.position = ref_goals[i_tracker].position + Vector3.forward + Vector3.up;
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
