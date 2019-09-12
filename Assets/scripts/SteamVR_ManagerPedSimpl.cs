//====== Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Enables/disables objects based on connectivity and assigned roles.
//
//=============================================================================

using UnityEngine;
using Valve.VR;
using RootMotion.FinalIK;
using System.Collections.Generic;

public class SteamVR_ManagerPedSimpl : SteamVR_Manager
{
	enum ObjType { tracker_rfoot = 2, tracker_lfoot, tracker_pelvis, tracker_rhand, tracker_lhand };
	string [] c_objNames = {
					"controller_right"
					, "controller_left"
					, "tracker_rfoot"
					, "tracker_lfoot"
					, "tracker_pelvis"
					, "tracker_rhand"
					, "tracker_lhand"
				};
	SteamVR_ManagerPedSimpl()
	{
		tracker_start = (int)ObjType.tracker_rfoot;
		tracker_end = (int)ObjType.tracker_lhand + 1;
		m_transition = new Transition[] {
									  new Transition(State.initial, State.pre_cnn, ALL)																													//1
									, new Transition(State.pre_cnn, State.pre_cnn, FORWARD, actInspecAvatar_f)																							//2
									, new Transition(State.pre_cnn, State.pre_cnn, RIGHT, actInspecAvatar_r)																							//3
									, new Transition(State.pre_cnn, State.pre_cnn, UP, actInspecAvatar_u)																								//4
									, new Transition(State.pre_cnn, State.pre_calibra, R_MENU, new Action[] {actIdentifyTrackers, actConnectVirtualWorld, actShowMirror, actInspecAvatar_f})			//5
									, new Transition(State.pre_cnn, State.pre_calibra, L_MENU, new Action[] {actIdentifyTrackers, actConnectVirtualWorld, actShowMirror, actInspecAvatar_f})			//6
									, new Transition(State.pre_calibra, State.pre_calibra, ALL, actAdjustMirror)																						//7
									, new Transition(State.pre_calibra, State.pre_calibra, FORWARD, actInspecAvatar_f)																					//8
									, new Transition(State.pre_calibra, State.pre_calibra, RIGHT, actInspecAvatar_r)																					//9
									, new Transition(State.pre_calibra, State.pre_calibra, UP, actInspecAvatar_u)																						//10
									, new Transition(State.pre_calibra, State.post_calibra, R_TRIGGER, actCalibration)																					//11
									, new Transition(State.pre_calibra, State.post_calibra, L_TRIGGER, actCalibration)																					//12
									, new Transition(State.post_calibra, State.post_calibra, ALL, actAdjustMirror)																						//13
									, new Transition(State.post_calibra, State.post_calibra, FORWARD, actInspecAvatar_f)																				//14
									, new Transition(State.post_calibra, State.post_calibra, RIGHT, actInspecAvatar_r)																					//15
									, new Transition(State.post_calibra, State.post_calibra, UP, actInspecAvatar_u)																						//16
									, new Transition(State.post_calibra, State.tracking, R_GRIP, new Action[]{ actUnShowMirror, actHideTracker })														//17
									, new Transition(State.post_calibra, State.pre_calibra, L_GRIP, actUnCalibration)																					//18
									, new Transition(State.post_calibra, State.pre_cnn, L_MENU|R_MENU, new Action[]{actUnShowMirror, actUnCalibration, actUnConnectVirtualWorld, actInspecAvatar_f})	//19
									, new Transition(State.tracking, State.pre_cnn, L_MENU|R_MENU, new Action[]{actUnHideTracker, actUnCalibration, actUnConnectVirtualWorld, actInspecAvatar_f})		//20
									, new Transition(State.tracking, State.teleporting, R_TRIGGER, actTeleportP)																						//21
									, new Transition(State.tracking, State.teleporting, L_TRIGGER, actTeleportM)																						//22
									, new Transition(State.teleporting, State.tracking, NONE)																											//23
									, new Transition(State.tracking, State.tracking, FORWARD, actInspecAvatar_f)																						//24
									, new Transition(State.tracking, State.tracking, RIGHT, actInspecAvatar_r)																							//25
									, new Transition(State.tracking, State.tracking, UP, actInspecAvatar_u)																								//26
								};
		g_inst = this;
	}

	public override bool IdentifyTrackers()
	{
		Transform ori = m_hmd.transform;
		GameObject [] trackers = new GameObject[5] {
			  m_objects[(int)ObjType.tracker_rfoot]
			, m_objects[(int)ObjType.tracker_lfoot]
			, m_objects[(int)ObjType.tracker_pelvis]
			, m_objects[(int)ObjType.tracker_rhand]
			, m_objects[(int)ObjType.tracker_lhand]
		};
		if (Tracker.IdentifyTrackers_5(trackers, ori))
		{
			for (int i_tracker = 0; i_tracker < trackers.Length; i_tracker ++)
			{
				int i_obj = i_tracker + tracker_start;
				m_objects[i_obj] = trackers[i_tracker];
				m_objects[i_obj].name = c_objNames[i_obj];
			}
			return true;
		}
		else
			return false;
	}

	public override bool Calibration()
	{
		VRIK ik = m_avatar.GetComponent<VRIK>();
		m_data = VRIKCalibrator2.Calibrate(ik, m_hmd.transform
					, m_objects[(int)ObjType.tracker_pelvis].transform
					, m_objects[(int)ObjType.tracker_lhand].transform
					, m_objects[(int)ObjType.tracker_rhand].transform
					, m_objects[(int)ObjType.tracker_lfoot].transform
					, m_objects[(int)ObjType.tracker_rfoot].transform);
		return true;
	}

	public override void UnCalibration()
	{
		VRIK ik = m_avatar.GetComponent<VRIK>();
		VRIKCalibrator2.UnCalibrate(ik, m_hmd.transform
					, m_objects[(int)ObjType.tracker_pelvis].transform
					, m_objects[(int)ObjType.tracker_lhand].transform
					, m_objects[(int)ObjType.tracker_rhand].transform
					, m_objects[(int)ObjType.tracker_lfoot].transform
					, m_objects[(int)ObjType.tracker_rfoot].transform);
	}

	public override void ConnectVirtualWorld()
	{
		Transform v = m_avatar.transform;
		Vector3 t_v = v.forward;
		Vector3 u_v = v.up;
		Vector3 r_v = v.right;
		Matrix4x4 m_v = new Matrix4x4(new Vector4(t_v.x, t_v.y, t_v.z, 0f)
									, new Vector4(u_v.x, u_v.y, u_v.z, 0f)
									, new Vector4(r_v.x, r_v.y, r_v.z, 0f)
									, new Vector4(0f, 0f, 0f, 1f));

		Matrix4x4 v2p = transform.worldToLocalMatrix;
		Vector3 t_p = v2p.MultiplyVector(m_hmd.transform.forward);
		Vector3 u_p = v2p.MultiplyVector(m_hmd.transform.up);
		Vector3 r_p = Vector3.Cross(u_p, t_p).normalized;

		Vector3 u_prime_p = u_v;
		Vector3 r_prime_p = Vector3.Cross(u_prime_p, t_p).normalized;
		Vector3 t_prime_p = Vector3.Cross(r_prime_p, u_prime_p).normalized;

		Matrix4x4 m_p = new Matrix4x4(new Vector4(t_prime_p.x, t_prime_p.y, t_prime_p.z, 0f)
									, new Vector4(u_prime_p.x, u_prime_p.y, u_prime_p.z, 0f)
									, new Vector4(r_prime_p.x, r_prime_p.y, r_prime_p.z, 0f)
									, new Vector4(0f, 0f, 0f, 1f));


		Matrix4x4 l = m_v.transpose * m_p;

		Vector3 o_p = (m_objects[(int)ObjType.tracker_lfoot].transform.localPosition + m_objects[(int)ObjType.tracker_rfoot].transform.localPosition) * 0.5f;
		o_p.y = 0.0f;
		Vector3 o_v = v.position;
		o_v.y = 0.0f;
		Vector3 t = -l.MultiplyVector(o_p) + o_v;

		Transport(l.rotation, t);
	}

	protected override void UpdateInstructionDisplay(State s)
	{
        Debug.Assert(null != m_refDispHeader);
        m_refDispHeader.text = StateStringsPed.s_shortDesc[(int)s];
        string body = StateStringsPed.s_longDesc[(int)s] + "\n";
        for (int i_tran = 0; i_tran < m_transition.Length; i_tran ++)
        {
        	string desc_tran = TransitionStringsPedSimpl.s_Desc[i_tran];
        	if (m_transition[i_tran].From == s
        		&& null != desc_tran)
        	{
        		body += "\n";
        		body += desc_tran;
        	}
        }
        m_refDispBody.text = body;
	}
}

