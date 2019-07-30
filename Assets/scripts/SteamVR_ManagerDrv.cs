//====== Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Enables/disables objects based on connectivity and assigned roles.
//
//=============================================================================

using UnityEngine;
using Valve.VR;
using RootMotion.FinalIK;
using System.Collections.Generic;

public class SteamVR_ManagerDrv : SteamVR_Manager
{
	class TransformDefault
	{
		Transform m_transform;
		Vector3 m_position;
		Quaternion m_rotation;
		Matrix4x4 m_T;

		public TransformDefault(Transform tran, Transform vTrackerInitial, Transform vTrackersDefault)
		{
			m_T = vTrackersDefault.localToWorldMatrix * vTrackerInitial.worldToLocalMatrix;
			m_position = m_T.MultiplyPoint3x4(tran.position);
			m_rotation =  m_T.rotation * tran.rotation;
			m_transform = tran;
		}

		public void Reset()
		{
			m_transform.position = m_position;
			m_transform.rotation =  m_rotation;
		}
	}

	enum ObjType { tracker_rfoot = 2, tracker_lfoot, tracker_pelvis, tracker_rhand, tracker_lhand };
	const int c_totalTrackers = 6;
	TransformDefault [] m_trackerDft = new TransformDefault[c_totalTrackers];
	[HideInInspector]
	public GameObject m_carHost;

	SteamVR_ManagerDrv()
	{
		tracker_start = (int)ObjType.tracker_rfoot;
		tracker_end = (int)ObjType.tracker_lhand + 1;
		m_transition = new Transition[] {
									  new Transition(State.initial, State.pre_transport, ALL)
									, new Transition(State.pre_transport, State.post_transport, R_TRIGGER, new Action[] {actIdentifyTrackers, actConnectVirtualWorld})
									, new Transition(State.pre_transport, State.post_transport, L_TRIGGER, new Action[] {actIdentifyTrackers, actConnectVirtualWorld})
									, new Transition(State.post_transport, State.pre_transport, L_GRIP, actUnConnectVirtualWorld)
									, new Transition(State.post_transport, State.pre_calibra, R_GRIP, actShowMirror)
									, new Transition(State.pre_calibra, State.pre_calibra, ALL, actAdjustMirror)
									, new Transition(State.pre_calibra, State.post_calibra, R_TRIGGER, actCalibration)
									, new Transition(State.pre_calibra, State.post_calibra, L_TRIGGER, actCalibration)
									, new Transition(State.post_calibra, State.post_calibra, ALL, actAdjustMirror)
									, new Transition(State.post_calibra, State.pegging, R_GRIP, new Action[]{ actUnShowMirror, actHideTracker, actPegLock })
									, new Transition(State.pegging, State.tracking, R_TRIGGER, new Action[]{ actPegUnLock4Tracking })
									, new Transition(State.pegging, State.tracking, L_TRIGGER, new Action[]{ actPegUnLock4Tracking })
									, new Transition(State.post_calibra, State.pre_calibra, L_GRIP, actUnCalibration)
									, new Transition(State.post_calibra, State.pre_transport, L_MENU|R_MENU, new Action[]{actUnShowMirror, actUnCalibration, actUnConnectVirtualWorld})
									, new Transition(State.tracking, State.pre_transport, L_MENU|R_MENU, new Action[]{actPegUnLock, actUnHideTracker, actUnCalibration, actUnConnectVirtualWorld})
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
			m_objects[(int)ObjType.tracker_rfoot] = trackers[0];
			m_objects[(int)ObjType.tracker_lfoot] = trackers[1];
			m_objects[(int)ObjType.tracker_pelvis] = trackers[2];
			m_objects[(int)ObjType.tracker_rhand] = trackers[3];
			m_objects[(int)ObjType.tracker_lhand] = trackers[4];
			return true;
		}
		else
			return false;
	}

	public override bool Calibration()
	{
		//fixme: hmd should be replaced with a tracker
		Transform [] trackers_p = new Transform[c_totalTrackers] {
			m_hmd.transform
			, m_objects[(int)ObjType.tracker_rfoot].transform
			, m_objects[(int)ObjType.tracker_lfoot].transform
			, m_objects[(int)ObjType.tracker_pelvis].transform
			, m_objects[(int)ObjType.tracker_rhand].transform
			, m_objects[(int)ObjType.tracker_lhand].transform
		};
		string [] names_t = new string[c_totalTrackers] {
			"head_vtracker_default"
			, "rfoot_vtracker_default"
			, "lfoot_vtracker_default"
			, "pelvis_vtracker_default"
			, "rhand_vtracker_default"
			, "lhand_vtracker_default"
		};
		Transform [] vtrackers_t = new Transform[c_totalTrackers];
		for (int i = 0; i < c_totalTrackers; i ++)
		{
			vtrackers_t[i] = m_carHost.transform.Find(names_t[i]);
			Debug.Assert(null != vtrackers_t[i]);
		}
		string [] names_s = new string[c_totalTrackers] {
			"head_vtracker"
			, "rfoot_vtracker"
			, "lfoot_vtracker"
			, "pelvis_vtracker"
			, "rhand_vtracker"
			, "lhand_vtracker"
		};
		Transform [] vtrackers_s = new Transform[c_totalTrackers];
		for (int i = 0; i < c_totalTrackers; i ++)
		{
			vtrackers_s[i] = m_avatar.transform.Find(names_s[i]);
			Debug.Assert(null != vtrackers_s[i]);
		}

		for (int i = 0; i < c_totalTrackers; i ++)
		{
			m_trackerDft[i] = new TransformDefault(trackers_p[i], vtrackers_s[i], vtrackers_t[i]);
		}

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
		Vector3 r_p = Vector3.Cross(u_p, t_p);

		Vector3 u_prime_p = u_v;
		Vector3 r_prime_p = Vector3.Cross(u_prime_p, t_p);
		Vector3 t_prime_p = Vector3.Cross(r_prime_p, u_prime_p);

		Matrix4x4 m_p = new Matrix4x4(new Vector4(t_prime_p.x, t_prime_p.y, t_prime_p.z, 0f)
									, new Vector4(u_prime_p.x, u_prime_p.y, u_prime_p.z, 0f)
									, new Vector4(r_prime_p.x, r_prime_p.y, r_prime_p.z, 0f)
									, new Vector4(0f, 0f, 0f, 1f));


		Matrix4x4 l = m_v.transpose * m_p;

		Vector3 o_p = (m_objects[(int)ObjType.tracker_lfoot].transform.localPosition + m_objects[(int)ObjType.tracker_rfoot].transform.localPosition) * 0.5f;
		o_p.y = 0.0f;
		Vector3 o_v = v.position;
		Vector3 t = -l.MultiplyVector(o_p) + o_v;

		Transport(l.rotation, t);
	}

	protected static bool actPegLock(uint cond)
	{
		GameObject [] trackers = new GameObject[] {
			g_inst.m_hmd
			, g_inst.m_objects[(int)ObjType.tracker_rfoot]
			, g_inst.m_objects[(int)ObjType.tracker_lfoot]
			, g_inst.m_objects[(int)ObjType.tracker_pelvis]
			, g_inst.m_objects[(int)ObjType.tracker_rhand]
			, g_inst.m_objects[(int)ObjType.tracker_lhand]
		};
		for (int i = 0; i < trackers.Length; i ++)
		{
			SteamVR_TrackedObject t = trackers[i].GetComponent<SteamVR_TrackedObject>();
			t.Lock(true);
		}

		foreach (TransformDefault dft in ((SteamVR_ManagerDrv)g_inst).m_trackerDft)
		{
			dft.Reset();
		}
		return true;
	}

	protected static bool actPegUnLock(uint cond)
	{
		//fixme: unlock trackers (except feet trackers) to recieve tracking transform update
		GameObject [] trackers = new GameObject[] {
			g_inst.m_hmd
			, g_inst.m_objects[(int)ObjType.tracker_rfoot]
			, g_inst.m_objects[(int)ObjType.tracker_lfoot]
			, g_inst.m_objects[(int)ObjType.tracker_pelvis]
			, g_inst.m_objects[(int)ObjType.tracker_rhand]
			, g_inst.m_objects[(int)ObjType.tracker_lhand]
		};
		for (int i = 0; i < trackers.Length; i ++)
		{
			SteamVR_TrackedObject t = trackers[i].GetComponent<SteamVR_TrackedObject>();
			t.Lock(false);
		}
		return true;
	}

	protected static bool actPegUnLock4Tracking(uint cond)
	{
		//fixme: unlock trackers (except feet trackers) to recieve tracking transform update
		//fixme: align virtual car with physical car
		GameObject [] trackers = new GameObject[] {
			g_inst.m_hmd
			, g_inst.m_objects[(int)ObjType.tracker_pelvis]
			, g_inst.m_objects[(int)ObjType.tracker_rhand]
			, g_inst.m_objects[(int)ObjType.tracker_lhand]
		};
		for (int i = 0; i < trackers.Length; i ++)
		{
			SteamVR_TrackedObject t = trackers[i].GetComponent<SteamVR_TrackedObject>();
			t.Lock(false);
		}
		return true;
	}
}

