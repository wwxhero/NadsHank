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
		Vector3 m_position = new Vector3(0, 0, 0);
		Quaternion m_rotation = new Quaternion(0, 0, 0, 1);
		Matrix4x4 m_T;
		public Vector3 position
		{
			get { return m_position; }
		}
		public Quaternion rotation
		{
			get { return m_rotation; }
		}
		public void Update(Transform tran, Transform vTrackerInitial, Transform vTrackersDefault)
		{
			m_T = vTrackersDefault.localToWorldMatrix * vTrackerInitial.worldToLocalMatrix;
			m_position = m_T.MultiplyPoint3x4(tran.position);
			m_rotation =  m_T.rotation * tran.rotation;
		}
	}

	enum ObjType { tracker_rhand = 2, tracker_lhand, tracker_pelvis, tracker_head, tracker_rfoot, tracker_lfoot };
	enum TrackerType { tracker_rhand = 0, tracker_lhand, tracker_pelvis, tracker_head, tracker_rfoot, tracker_lfoot };
	string [] c_trackerNames = {
				"tracker_rhand"
				, "tracker_lhand"
				, "tracker_pelvis"
				, "tracker_head"
				, "tracker_rfoot"
				, "tracker_lfoot"
			};
	string [] c_vtrackerAvatarNames = {
				"rhand_vtracker"
				, "lhand_vtracker"
				, "pelvis_vtracker"
				, "head_vtracker"
				, "rfoot_vtracker"
				, "lfoot_vtracker"
			};
	string [] c_vtrackerCarNames = {
				"rhand_vtracker_default"
				, "lhand_vtracker_default"
				, "pelvis_vtracker_default"
				, "head_vtracker_default"
				, "rfoot_vtracker_default"
				, "lfoot_vtracker_default"
			};
	const int c_totalTrackers = 6;
	[HideInInspector]
	public GameObject m_carHost;

	SteamVR_ManagerDrv()
	{
		tracker_start = (int)ObjType.tracker_rhand;
		tracker_end = (int)ObjType.tracker_lfoot + 1;
		m_transition = new Transition[] {
									  new Transition(State.initial, State.pre_cnn, ALL)
									, new Transition(State.pre_cnn, State.post_cnn, R_TRIGGER, new Action[] {actIdentifyTrackers, actConnectVirtualWorld})
									, new Transition(State.pre_cnn, State.post_cnn, L_TRIGGER, new Action[] {actIdentifyTrackers, actConnectVirtualWorld})
									, new Transition(State.post_cnn, State.pre_cnn, L_GRIP, actUnConnectVirtualWorld)
									, new Transition(State.post_cnn, State.pre_calibra, R_GRIP, actShowMirror)
									, new Transition(State.pre_calibra, State.pre_calibra, ALL, actAdjustMirror)
									, new Transition(State.pre_calibra, State.pre_calibra, ALL, actAdjustAvatar)
									, new Transition(State.pre_calibra, State.pre_calibra, FORWARD, actAdjustAvatarInspec_f)
									, new Transition(State.pre_calibra, State.pre_calibra, RIGHT, actAdjustAvatarInspec_r)
									, new Transition(State.pre_calibra, State.pre_calibra, UP, actAdjustAvatarInspec_u)
									, new Transition(State.pre_calibra, State.pre_calibra2, R_TRIGGER, actPosTrackerLock)
									, new Transition(State.pre_calibra, State.pre_calibra2, L_TRIGGER, actPosTrackerLock)
									, new Transition(State.pre_calibra2, State.pre_calibra, L_GRIP, actPosTrackerUnLock)
									, new Transition(State.pre_calibra2, State.pre_calibra2, FORWARD, actAdjustAvatarInspec_f)
									, new Transition(State.pre_calibra2, State.pre_calibra2, RIGHT, actAdjustAvatarInspec_r)
									, new Transition(State.pre_calibra2, State.pre_calibra2, UP, actAdjustAvatarInspec_u)
									, new Transition(State.pre_calibra2, State.post_calibra, L_MENU, new Action[] {actCalibration, actPosTrackerUnLock})
									, new Transition(State.pre_calibra2, State.post_calibra, R_MENU, new Action[] {actCalibration, actPosTrackerUnLock})
									, new Transition(State.post_calibra, State.post_calibra, ALL, actAdjustMirror)
									, new Transition(State.post_calibra, State.post_calibra, FORWARD, actAdjustAvatarInspec_f)
									, new Transition(State.post_calibra, State.post_calibra, RIGHT, actAdjustAvatarInspec_r)
									, new Transition(State.post_calibra, State.post_calibra, UP, actAdjustAvatarInspec_u)
									, new Transition(State.post_calibra, State.pegging, R_GRIP, new Action[]{ actUnShowMirror, actPegLock })
									, new Transition(State.pegging, State.tracking, R_TRIGGER, new Action[] { actPegUnLock4Tracking, actAdjustVWCnn })
									, new Transition(State.pegging, State.tracking, L_TRIGGER, new Action[] { actPegUnLock4Tracking, actAdjustVWCnn })
									, new Transition(State.tracking, State.tracking, R_GRIP, actAdjustVWCnn)
									, new Transition(State.tracking, State.tracking, L_GRIP, actAdjustVWCnn)
									//sub state transitions for adjust avatar in forward/backward direction
									, new Transition(State.tracking, State.pre_adjF, FORWARD, actAdjustCarInspec_u)
									, new Transition(State.pre_adjF, State.post_adjF, FORWARD|PLUS, actAvatarAdjF_p)
									, new Transition(State.pre_adjF, State.post_adjF, FORWARD|MINUS, actAvatarAdjF_m)
									, new Transition(State.post_adjF, State.pre_adjF, FORWARD)
									, new Transition(State.post_adjF, State.tracking, NONE, actAdjustCarInspec_f)
									, new Transition(State.pre_adjF, State.tracking, NONE, actAdjustCarInspec_f)
									//sub state transitions for adjust avatar in right/left direction
									, new Transition(State.tracking, State.pre_adjR, RIGHT, actAdjustCarInspec_r)
									, new Transition(State.pre_adjR, State.post_adjR, RIGHT|PLUS, actAvatarAdjR_p)
									, new Transition(State.pre_adjR, State.post_adjR, RIGHT|MINUS, actAvatarAdjR_m)
									, new Transition(State.post_adjR, State.pre_adjR, RIGHT)
									, new Transition(State.post_adjR, State.tracking, NONE, actAdjustCarInspec_f)
									, new Transition(State.pre_adjR, State.tracking, NONE, actAdjustCarInspec_f)
									//sub state transitions for adjust avatar in up/down direction
									, new Transition(State.tracking, State.pre_adjU, UP, actAdjustCarInspec_f)
									, new Transition(State.pre_adjU, State.post_adjU, UP|PLUS, actAvatarAdjU_p)
									, new Transition(State.pre_adjU, State.post_adjU, UP|MINUS, actAvatarAdjU_m)
									, new Transition(State.post_adjU, State.pre_adjU, UP)
									, new Transition(State.post_adjU, State.tracking, NONE, actAdjustCarInspec_f)
									, new Transition(State.pre_adjU, State.tracking, NONE, actAdjustCarInspec_f)
									//sub state transitions for adjust avatar orientation
									, new Transition(State.tracking, State.pre_adjO, ORI, actAdjustCarInspec_f)
									, new Transition(State.pre_adjO, State.post_adjO, ORI|PLUS, actAvatarAdjO_p)
									, new Transition(State.pre_adjO, State.post_adjO, ORI|MINUS, actAvatarAdjO_m)
									, new Transition(State.post_adjO, State.pre_adjO, ORI)
									, new Transition(State.post_adjO, State.tracking, NONE, actAdjustCarInspec_r)
									, new Transition(State.pre_adjO, State.tracking, NONE, actAdjustCarInspec_r)

									, new Transition(State.post_calibra, State.pre_calibra, L_GRIP, actUnCalibration)
									, new Transition(State.post_calibra, State.pre_cnn, L_MENU|R_MENU, new Action[]{actUnShowMirror, actUnCalibration, actUnConnectVirtualWorld})
									, new Transition(State.tracking, State.pre_cnn, L_MENU|R_MENU, new Action[]{actPegUnLock, actUnCalibration, actUnConnectVirtualWorld})

								};
		g_inst = this;
	}

	public override bool IdentifyTrackers()
	{
		//4 real trackers: lhand, rhand, pelvis, head
		//1 hmd
		Transform ori = m_hmd.transform;
		GameObject [] trackers = new GameObject[] {
			  m_objects[(int)ObjType.tracker_pelvis]
			, m_objects[(int)ObjType.tracker_rhand]
			, m_objects[(int)ObjType.tracker_lhand]
			, m_objects[(int)ObjType.tracker_head]
		};
		bool trackers_ready = true;
		for (int i = 0; i < trackers.Length && trackers_ready; i ++)
			trackers_ready = trackers[i].activeSelf;

		if (trackers_ready && Tracker.IdentifyTrackers_4(trackers, ori))
		{
			m_objects[(int)ObjType.tracker_rhand] = trackers[0];
			m_objects[(int)ObjType.tracker_lhand] = trackers[1];
			m_objects[(int)ObjType.tracker_pelvis] = trackers[2];
			m_objects[(int)ObjType.tracker_head] = trackers[3];
			trackers[0].name = c_trackerNames[(int)TrackerType.tracker_rhand];
			trackers[1].name = c_trackerNames[(int)TrackerType.tracker_lhand];
			trackers[2].name = c_trackerNames[(int)TrackerType.tracker_pelvis];
			trackers[3].name = c_trackerNames[(int)TrackerType.tracker_head];
			return true;
		}
		else
			return false;
	}

	public override bool Calibration()
	{
		TransformDefault dft =  new TransformDefault();
		for (int i_tracker = 0; i_tracker < c_totalTrackers; i_tracker ++)
		{
			int i_obj = i_tracker + tracker_start;
			Transform tracker = m_objects[i_obj].transform;
			Transform vtracker = m_avatar.transform.Find(c_vtrackerAvatarNames[i_tracker]);
			Transform vtracker_prime = m_carHost.transform.Find(c_vtrackerCarNames[i_tracker]);
			Debug.Assert(null != tracker && null != vtracker && null != vtracker_prime);
			dft.Update(tracker, vtracker, vtracker_prime);
			m_objects[i_obj].GetComponent<SteamVR_TrackedObject>().SetDft(dft.position, dft.rotation);
		}

		VRIK ik = m_avatar.GetComponent<VRIK>();
		m_data = VRIKCalibrator2.Calibrate(ik
					, m_objects[(int)ObjType.tracker_head].transform
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
		VRIKCalibrator2.UnCalibrate(ik
					, m_objects[(int)ObjType.tracker_head].transform
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

		Vector3 o_p = (m_objects[(int)ObjType.tracker_lhand].transform.localPosition + m_objects[(int)ObjType.tracker_rhand].transform.localPosition) * 0.5f;
		o_p.y = 0.0f;
		Vector3 o_v = v.position;
		o_v.y = 0.0f;
		Vector3 t = -l.MultiplyVector(o_p) + o_v;

		Transport(l.rotation, t);

		Transform [] vtrackers = new Transform[] {
			m_avatar.transform.Find(c_vtrackerAvatarNames[(int)TrackerType.tracker_lfoot])
			, m_avatar.transform.Find(c_vtrackerAvatarNames[(int)TrackerType.tracker_rfoot])
		};
		SteamVR_TrackedObject [] ftrackers = new SteamVR_TrackedObject[] {
			m_objects[(int)ObjType.tracker_lfoot].GetComponent<SteamVR_TrackedObject>()
			, m_objects[(int)ObjType.tracker_rfoot].GetComponent<SteamVR_TrackedObject>()
		};
		for (int i_ftrack = 0; i_ftrack < ftrackers.Length; i_ftrack ++)
		{
			ftrackers[i_ftrack].Lock(vtrackers[i_ftrack].position, vtrackers[i_ftrack].rotation);
			var obj = ftrackers[i_ftrack].gameObject;
			//Debug.Assert(!obj.activeSelf);
			obj.SetActive(true);
		}
	}

	private static bool actPosTrackerLock(uint cond)
	{
		GameObject [] trackers = new GameObject[] {
			  g_inst.m_objects[(int)ObjType.tracker_rfoot]
			, g_inst.m_objects[(int)ObjType.tracker_lfoot]
			, g_inst.m_objects[(int)ObjType.tracker_pelvis]
			, g_inst.m_objects[(int)ObjType.tracker_rhand]
			, g_inst.m_objects[(int)ObjType.tracker_lhand]
		};

		for (int i = 0; i < trackers.Length; i ++)
		{
			SteamVR_TrackedObject t = trackers[i].GetComponent<SteamVR_TrackedObject>();
			Transform dft = trackers[i].transform;
			t.Lock(dft.position, dft.rotation);
		}

		return true;
	}

	private static bool actPosTrackerUnLock(uint cond)
	{
		GameObject [] trackers = new GameObject[] {
			  g_inst.m_objects[(int)ObjType.tracker_rfoot]
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

	protected static bool actPegLock(uint cond)
	{
		GameObject [] trackers = new GameObject[] {
			  g_inst.m_objects[(int)ObjType.tracker_head]
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

		return true;
	}

	protected static bool actPegUnLock(uint cond)
	{
		GameObject [] trackers = new GameObject[] {
			  g_inst.m_objects[(int)ObjType.tracker_head]
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
		GameObject [] trackers = new GameObject[] {
			  g_inst.m_objects[(int)ObjType.tracker_head]
			, g_inst.m_objects[(int)ObjType.tracker_pelvis]
			, g_inst.m_objects[(int)ObjType.tracker_rhand]
			, g_inst.m_objects[(int)ObjType.tracker_lhand]
		};
		bool all_trackers_unlocked = true;
		for (int i = 0; i < trackers.Length; i ++)
		{
			SteamVR_TrackedObject t = trackers[i].GetComponent<SteamVR_TrackedObject>();
			t.Lock(false);		//trackers are unlocked with latency
			all_trackers_unlocked = all_trackers_unlocked && !t.Locked();
		}
		g_inst.Avatar.GetComponent<VRIK>().LockSolver(!all_trackers_unlocked);

		return all_trackers_unlocked;
	}

	protected static bool actAdjustVWCnn(uint cond)
	{
		SteamVR_ManagerDrv pThis = (SteamVR_ManagerDrv)g_inst;
		Vector3 hands = (pThis.m_objects[(int)ObjType.tracker_rhand].transform.position
						+ pThis.m_objects[(int)ObjType.tracker_lhand].transform.position) * 0.5f;
		Vector3 hands_prime = (pThis.m_carHost.transform.Find(pThis.c_vtrackerCarNames[(int)TrackerType.tracker_rhand]).position
								+ pThis.m_carHost.transform.Find(pThis.c_vtrackerCarNames[(int)TrackerType.tracker_lhand]).position) * 0.5f;
		Vector3 translate = hands_prime - hands;
		translate.y = 0; //only just in plane (x, z)
		pThis.transform.position = pThis.transform.position + translate;
		return true;
	}

	protected static bool actAdjustCarInspec_r(uint cond)
	{
		ScenarioControl sc = g_inst.m_senarioCtrl.GetComponent<ScenarioControl>();
		sc.adjustInspector(ScenarioControl.InspectorHelper.Direction.right, true);
		return true;
	}

	protected static bool actAdjustCarInspec_u(uint cond)
	{
		ScenarioControl sc = g_inst.m_senarioCtrl.GetComponent<ScenarioControl>();
		sc.adjustInspector(ScenarioControl.InspectorHelper.Direction.up, true);
		return true;
	}

	protected static bool actAdjustCarInspec_f(uint cond)
	{
		ScenarioControl sc = g_inst.m_senarioCtrl.GetComponent<ScenarioControl>();
		sc.adjustInspector(ScenarioControl.InspectorHelper.Direction.forward, true);
		return true;
	}

	const float c_deltaT = 0.005f;
	const float c_deltaR = 0.5f; //in degree
	protected static bool actAvatarAdjF_m(uint cond)
	{
		Vector3 deltaT = new Vector3(0, 0, -c_deltaT);
		((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
		return true;
	}

	protected static bool actAvatarAdjF_p(uint cond)
	{
		Vector3 deltaT = new Vector3(0, 0, c_deltaT);
		((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
		return true;
	}

	protected static bool actAvatarAdjR_m(uint cond)
	{
		Vector3 deltaT = new Vector3(-c_deltaT, 0, 0);
		((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
		return true;
	}

	protected static bool actAvatarAdjR_p(uint cond)
	{
		Vector3 deltaT = new Vector3(c_deltaT, 0, 0);
		((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
		return true;
	}

	protected static bool actAvatarAdjU_m(uint cond)
	{
		Vector3 deltaT = new Vector3(0, -c_deltaT, 0);
		((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
		return true;
	}

	protected static bool actAvatarAdjU_p(uint cond)
	{
		Vector3 deltaT = new Vector3(0, c_deltaT, 0);
		((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
		return true;
	}

	protected static bool actAvatarAdjO_m(uint cond)
	{
		((SteamVR_ManagerDrv)g_inst).adjustAvatar_r(-c_deltaR);
		return true;
	}

	protected static bool actAvatarAdjO_p(uint cond)
	{
		((SteamVR_ManagerDrv)g_inst).adjustAvatar_r(c_deltaR);
		return true;
	}
}

