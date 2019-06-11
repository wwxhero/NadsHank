using UnityEngine;
using System.Collections;
using RootMotion.FinalIK;

namespace RootMotion.Demos
{

    public class VRIKCalibrationController : MonoBehaviour
    {
        public bool calibration_done = false;
        [Tooltip("Reference to the VRIK component on the avatar.")] public VRIK ik;
        [Tooltip("The settings for VRIK calibration.")] public VRIKCalibrator.Settings settings;
        [Tooltip("The HMD.")] public Transform headTracker;
        [Tooltip("(Optional) A tracker placed anywhere on the body of the player, preferrably close to the pelvis, on the belt area.")] public Transform bodyTracker;
        [Tooltip("(Optional) A tracker or hand controller device placed anywhere on or in the player's left hand.")] public Transform leftHandTracker;
        [Tooltip("(Optional) A tracker or hand controller device placed anywhere on or in the player's right hand.")] public Transform rightHandTracker;
        [Tooltip("(Optional) A tracker placed anywhere on the ankle or toes of the player's left leg.")] public Transform leftFootTracker;
        [Tooltip("(Optional) A tracker placed anywhere on the ankle or toes of the player's right leg.")] public Transform rightFootTracker;

        [Header("Data stored by Calibration")]
        public VRIKCalibrator.CalibrationData data = new VRIKCalibrator.CalibrationData();

        void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                // Calibrate the character, store data of the calibration
                data = VRIKCalibrator.Calibrate(ik, settings, headTracker, bodyTracker, leftHandTracker, rightHandTracker, leftFootTracker, rightFootTracker);

                calibration_done = true;
            }

            /*
			 * calling Calibrate with settings will return a VRIKCalibrator.CalibrationData, which can be used to calibrate that same character again exactly the same in another scene (just pass data instead of settings),
			 * without being dependent on the pose of the player at calibration time.
			 * Calibration data still depends on bone orientations though, so the data is valid only for the character that it was calibrated to or characters with identical bone structures.
			 * If you wish to use more than one character, it would be best to calibrate them all at once and store the CalibrationData for each one.
			 * */
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (data.scale == 0f)
                {
                    Debug.LogError("No Calibration Data to calibrate to, please calibrate with settings first.");
                }
                else
                {
                    // Use data from a previous calibration to calibrate that same character again.
                    VRIKCalibrator.Calibrate(ik, data, headTracker, bodyTracker, leftHandTracker, rightHandTracker, leftFootTracker, rightFootTracker);
                }
            }

            // Recalibrates avatar scale only. Can be called only if the avatar has been calibrated already.
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (data.scale == 0f)
                {
                    Debug.LogError("Avatar needs to be calibrated before RecalibrateScale is called.");
                }
                VRIKCalibrator.RecalibrateScale(ik, settings);
            }
        }
        bool [] m_donecali = new bool[6] {false, false, false, false, false, false};
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                data = VRIKCalibrator2.Calibrate(ik, headTracker, bodyTracker, leftHandTracker, rightHandTracker, leftFootTracker, rightFootTracker);
            }
            else if(Input.GetKeyDown(KeyCode.G))
            {
                if (VRIKCalibrator2.CalibrateStem(ik, headTracker, bodyTracker, leftFootTracker, rightFootTracker, data))
                {
                    m_donecali[0] = true; m_donecali[1] = true; m_donecali[4] = true; m_donecali[5] = true;
                }
            }
            else if((m_donecali[0] == true
                    && m_donecali[1] == true
                    && m_donecali[2] == false
                    //&& m_donecali[3] == true
                    && m_donecali[4] == true
                    && m_donecali[5] == true)
                    && Input.GetKeyDown(KeyCode.H)) //calibrate for left hand
            {
                m_donecali[2] = VRIKCalibrator2.CalibrateLeftHand(ik, leftHandTracker, data);
            }
            else if((m_donecali[0] == true
                    && m_donecali[1] == true
                    //&& m_donecali[2] == true
                    && m_donecali[3] == false
                    && m_donecali[4] == true
                    && m_donecali[5] == true)
                    && Input.GetKeyDown(KeyCode.I)) //calibrate for right hand
            {
                m_donecali[3] = VRIKCalibrator2.CalibrateRightHand(ik, rightHandTracker, data);
            }

            if (calibration_done && false)
            {
                Transform[] iks = new Transform[] {
                                                              ik.solver.spine.headTarget
                                                            , ik.solver.spine.pelvisTarget
                                                            , ik.solver.leftArm.target
                                                            , ik.solver.rightArm.target
                                                            , ik.solver.leftLeg.target
                                                            , ik.solver.rightLeg.target
                                                };
                Transform[] src = new Transform[] {
                                                              ik.references.head
                                                            , ik.references.pelvis
                                                            , ik.references.leftHand
                                                            , ik.references.rightHand
                                                            , ik.references.leftToes
                                                            , ik.references.rightToes
                                                };
                string log = null;
                for (int i = 0; i < iks.Length; i++)
                {
                    log += string.Format("\n{0}:\n\t[{1,6:#.00} {2,6:#.00} {3,6:#.00}] [{4,6:#.00} {5,6:#.00} {6,6:#.00}]\n\t[{7,6:#.00} {8,6:#.00} {9,6:#.00}] [{10,6:#.00} {11,6:#.00} {12,6:#.00}]"
                                            , src[i].name
                                            , iks[i].position.x, iks[i].position.y, iks[i].position.z, src[i].position.x, src[i].position.y, src[i].position.z
                                            , iks[i].eulerAngles.x, iks[i].eulerAngles.y, iks[i].eulerAngles.z, src[i].eulerAngles.x, src[i].eulerAngles.y, src[i].eulerAngles.z);
                }
                Debug.Log(log);
            }
        }
    }
}
