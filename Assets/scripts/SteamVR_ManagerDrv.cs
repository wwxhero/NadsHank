//====== Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Enables/disables objects based on connectivity and assigned roles.
//
//=============================================================================

using UnityEngine;
using Valve.VR;
using RootMotion.FinalIK;
using System.Collections.Generic;
using System;

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

	enum ObjType { tracker_rhand = 2, tracker_lhand, tracker_head, tracker_rfoot, tracker_lfoot, tracker_pelvis };
	enum TrackerType { tracker_rhand = 0, tracker_lhand, tracker_head, tracker_rfoot, tracker_lfoot, tracker_pelvis };
	string [] c_trackerNames = {
				"tracker_rhand"
				, "tracker_lhand"
				, "tracker_head"
				, "tracker_rfoot"
				, "tracker_lfoot"
				, "tracker_pelvis"
			};
	string [] c_vtrackerAvatarNames = {
				"rhand_vtracker"
				, "lhand_vtracker"
				, "head_vtracker"
				, "rfoot_vtracker"
				, "lfoot_vtracker"
				, "pelvis_vtracker"
			};
	string [] c_vtrackerCarNames = {
				"rhand_vtracker_default"
				, "lhand_vtracker_default"
				, "head_vtracker_default"
				, "rfoot_vtracker_default"
				, "lfoot_vtracker_default"
				, "pelvis_vtracker_default"
			};
	const int c_totalTrackers = 6;
	public bool m_simplified = false;
	[HideInInspector]
	public GameObject m_carHost;

	SteamVR_ManagerDrv()
	{
		tracker_start = (int)ObjType.tracker_rhand;
		tracker_end = (int)ObjType.tracker_lfoot + 1;
		m_transition = new Transition[] {
									  new Transition(State.initial, State.pre_cnn, R_TRIGGER, new Action[] {actGroundEle, actPersonpanelUpdateF })																							//1
									, new Transition(State.pre_cnn, State.pre_calibra, R_GRIP, new Action[] {actIdentifyTrackers, actConnectVirtualWorld, actShowMirror, actPersonpanelUpdateT, actInspecAvatar_f})		//2
									, new Transition(State.pre_calibra, State.pre_calibra, ALL, actAdjustMirror)																					//3
									, new Transition(State.pre_calibra, State.pre_calibra, FORWARD, actInspecAvatar_f)																				//4
									, new Transition(State.pre_calibra, State.pre_calibra, RIGHT, actInspecAvatar_r)																				//5
									, new Transition(State.pre_calibra, State.pre_calibra, UP, actInspecAvatar_u)																					//6
									, new Transition(State.pre_calibra, State.pre_calibra2, R_TRIGGER, actPosTrackerLock)																			//7
									, new Transition(State.pre_calibra2, State.pre_calibra, L_GRIP, actPosTrackerUnLock)																			//10
									, new Transition(State.pre_calibra2, State.pre_calibra2, FORWARD, actInspecAvatar_f)																			//11
									, new Transition(State.pre_calibra2, State.pre_calibra2, RIGHT, actInspecAvatar_r)																				//12
									, new Transition(State.pre_calibra2, State.pre_calibra2, UP, actInspecAvatar_u)																					//13
									, new Transition(State.pre_calibra2, State.post_calibra, R_MENU, new Action[] {actCalibration, actPosTrackerUnLock})											//14
									, new Transition(State.post_calibra, State.post_calibra, ALL, actAdjustIK_head)																					//16
									, new Transition(State.post_calibra, State.post_calibra, FORWARD, actInspecAvatar_f)																			//17
									, new Transition(State.post_calibra, State.post_calibra, RIGHT, actInspecAvatar_r)																				//18
									, new Transition(State.post_calibra, State.post_calibra, UP, actInspecAvatar_u)																					//19
									, new Transition(State.post_calibra, State.pre_calibra, L_GRIP, actUnCalibration)																				//19.1
									, new Transition(State.post_calibra, State.pegging, R_GRIP, new Action[]{ actUnShowMirror, actPegLock, actInspecCar_r })										//20
									, new Transition(State.pegging, State.adjusting_r, R_TRIGGER, new Action[]{ actPegUnLock4Tracking, actAdjustVWCnn })											//21
									, new Transition(State.adjusting_r, State.adjusting_r, L_GRIP, actAdjustVWCnn)																					//22
									, new Transition(State.adjusting_u, State.adjusting_u, L_GRIP, actAdjustVWCnn)																					//23
									, new Transition(State.adjusting_f, State.adjusting_f, L_GRIP, actAdjustVWCnn)																					//24
									, new Transition(State.adjusting_r, State.adjusting_u, UP, actInspecCar_u)																						//25
									, new Transition(State.adjusting_r, State.adjusting_f, FORWARD, actInspecCar_f)																					//26
									, new Transition(State.adjusting_u, State.adjusting_r, RIGHT, actInspecCar_r)																					//27
									, new Transition(State.adjusting_u, State.adjusting_f, FORWARD, actInspecCar_f)																					//28
									, new Transition(State.adjusting_f, State.adjusting_u, UP, actInspecCar_u)																						//29
									, new Transition(State.adjusting_f, State.adjusting_r, RIGHT, actInspecCar_r)																					//30
									, new Transition(State.adjusting_r, State.adjusting_r, ALL, actAdjustCar_r)																						//31
									, new Transition(State.adjusting_u, State.adjusting_u, ALL, actAdjustCar_u)																						//32
									, new Transition(State.adjusting_f, State.adjusting_f, ALL, actAdjustCar_f)																						//33
									, new Transition(State.adjusting_r, State.tracking, R_GRIP)																										//34
									, new Transition(State.adjusting_f, State.tracking, R_GRIP)																										//35
									, new Transition(State.adjusting_u, State.tracking, R_GRIP)																										//36
									, new Transition(State.tracking, State.pre_cnn, L_MENU|L_ARROW, new Action[]{actPegUnLock, actUnCalibration, actUnConnectVirtualWorld, actPersonpanelUpdateF, actInspecAvatar_f})		//37
								};
		g_inst = this;
	}

	public override bool IdentifyTrackers()
	{
		//4 real trackers: lhand, rhand, pelvis, head
		//1 hmd
		Transform ori = m_hmd.transform;
		GameObject [] trackers = new GameObject[] {
			  m_objects[(int)ObjType.tracker_rhand]
			, m_objects[(int)ObjType.tracker_lhand]
			, m_objects[(int)ObjType.tracker_head]
			, m_objects[(int)ObjType.tracker_rfoot]
			, m_objects[(int)ObjType.tracker_lfoot]
		};
		bool trackers_ready = true;
		for (int i = 0; i < trackers.Length && trackers_ready; i ++)
			trackers_ready = trackers[i].activeSelf;

		if (trackers_ready && Tracker.IdentifyTrackers_5Drv(trackers, ori))
		{
			m_objects[(int)ObjType.tracker_rhand] = trackers[0];
			m_objects[(int)ObjType.tracker_lhand] = trackers[1];
			m_objects[(int)ObjType.tracker_head] = trackers[2];
			m_objects[(int)ObjType.tracker_rfoot] = trackers[3];
			m_objects[(int)ObjType.tracker_lfoot] = trackers[4];
			trackers[0].name = c_trackerNames[(int)TrackerType.tracker_rhand];
			trackers[1].name = c_trackerNames[(int)TrackerType.tracker_lhand];
			trackers[2].name = c_trackerNames[(int)TrackerType.tracker_head];
			trackers[3].name = c_trackerNames[(int)TrackerType.tracker_rfoot];
			trackers[4].name = c_trackerNames[(int)TrackerType.tracker_lfoot];
			return true;
		}
		else
		{
			Exception e = new Exception("Indentify trackers failed, \r\n\tconfirm if all the trackers are valid, \r\n\tthe head tracker is hanging on leftside, \r\n\tand the participant is standing in 'T' posture!");
			throw e;
			return false;
		}
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
		//fixme: this is a workaround solution for head tracker
		//		, after removing HMD, align head in direction of x-y is difficult
		float hlp_z = m_data.head.localPosition.z;
		m_data.head.localPosition.Set(0f, 0f, hlp_z);
		VRIKCalibrator.Calibrate(ik
					, m_data
					, m_objects[(int)ObjType.tracker_head].transform
					, m_objects[(int)ObjType.tracker_pelvis].transform
					, m_objects[(int)ObjType.tracker_lhand].transform
					, m_objects[(int)ObjType.tracker_rhand].transform
					, m_objects[(int)ObjType.tracker_lfoot].transform
					, m_objects[(int)ObjType.tracker_rfoot].transform);
		return true;
	}

	public static bool actAdjustIK_head(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			//Debug.LogWarning("SteamVR_ManagerDrv::actAdjustIK_head");
			return false;
		}
		else
		{
			float delta_y = 0f;
			if (D_ARROW == cond)
				delta_y = -0.005f;
			else if (U_ARROW == cond)
				delta_y = +0.005f;
			if (0 != delta_y)
			{
				VRIK ik = ((SteamVR_ManagerDrv)g_inst).m_avatar.GetComponent<VRIK>();
				ik.solver.spine.headTarget.Translate(0f, delta_y, 0f, Space.World);
				((SteamVR_ManagerDrv)g_inst).m_data.head.localPosition = ik.solver.spine.headTarget.localPosition;
				return true;
			}
			else
				return false;
		}
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
			//m_objects[(int)ObjType.tracker_lfoot].GetComponent<SteamVR_TrackedObject>()
			//, m_objects[(int)ObjType.tracker_rfoot].GetComponent<SteamVR_TrackedObject>()
			m_objects[(int)ObjType.tracker_pelvis].GetComponent<SteamVR_TrackedObject>()
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
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("SteamVR_ManagerDrv::actPosTrackerLock");
			return true;
		}
		else
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
	}

	private static bool actPosTrackerUnLock(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("SteamVR_ManagerDrv::actPosTrackerUnLock");
			return true;
		}
		else
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
	}

	protected static bool actPegLock(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("SteamVR_ManagerDrv::actPegLock");
			return true;
		}
		else
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
	}

	protected static bool actPegUnLock(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("SteamVR_ManagerDrv::actPegUnLock");
			return true;
		}
		else
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
	}

	protected static bool actPegUnLock4Tracking(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("SteamVR_ManagerDrv::actPegUnLock4Tracking");
			return true;
		}
		else
		{
			GameObject [] trackers = new GameObject[] {
				  g_inst.m_objects[(int)ObjType.tracker_head]
				, g_inst.m_objects[(int)ObjType.tracker_rhand]
				, g_inst.m_objects[(int)ObjType.tracker_lhand]
			};
			for (int i = 0; i < trackers.Length; i ++)
			{
				SteamVR_TrackedObject t = trackers[i].GetComponent<SteamVR_TrackedObject>();
				t.Lock(false);		//trackers are unlocked with latency
			}
			return true;
		}
	}

	protected static bool actAdjustVWCnn(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("SteamVR_ManagerDrv::actAdjustVWCnn");
			return true;
		}
		else
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
	}

	protected static bool actInspecCar_r(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			//Debug.LogWarning("SteamVR_ManagerDrv::actInspecCar_r");
			return true;
		}
		else
		{
			ScenarioControl sc = g_inst.m_senarioCtrl.GetComponent<ScenarioControl>();
			sc.adjustInspector(ScenarioControl.InspectorHelper.Direction.right, ScenarioControl.InspectorHelper.ObjType.Host);
			return true;
		}
	}

	protected static bool actInspecCar_u(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			//Debug.LogWarning("SteamVR_ManagerDrv::actInspecCar_u");
			return true;
		}
		else
		{
			ScenarioControl sc = g_inst.m_senarioCtrl.GetComponent<ScenarioControl>();
			sc.adjustInspector(ScenarioControl.InspectorHelper.Direction.up, ScenarioControl.InspectorHelper.ObjType.Host);
			return true;
		}
	}

	protected static bool actInspecCar_f(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			//Debug.LogWarning("SteamVR_ManagerDrv::actInspecCar_f");
			return true;
		}
		else
		{
			ScenarioControl sc = g_inst.m_senarioCtrl.GetComponent<ScenarioControl>();
			sc.adjustInspector(ScenarioControl.InspectorHelper.Direction.forward, ScenarioControl.InspectorHelper.ObjType.Host);
			return true;
		}
	}

	const float c_deltaT = 0.005f;
	const float c_deltaR = 0.5f; //in degree

	protected static bool actAdjustCar_r(uint cond)
	{
		Vector3 deltaT = new Vector3(0, 0, 0);
		bool adjusting = false;
		switch(cond)
		{
		case R_ARROW:
			deltaT.z = c_deltaT;
			adjusting = true;
			break;
		case L_ARROW:
			deltaT.z = -c_deltaT;
			adjusting = true;
			break;
		case U_ARROW:
			deltaT.y = c_deltaT;
			adjusting = true;
			break;
		case D_ARROW:
			deltaT.y = -c_deltaT;
			adjusting = true;
			break;
		}
		if (adjusting)
		{
			if (g_inst.DEF_MOCKSTEAM)
				Debug.LogWarning("SteamVR_ManagerDrv::actAdjustCar_r");
			else
				((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
			return true;
		}
		else
			return false;
	}

	protected static bool actAdjustCar_u(uint cond)
	{
		Vector3 deltaT = new Vector3(0, 0, 0);
		float deltaR = 0;

		bool adjusting_tran = false;
		bool adjusting_rot = false;
		switch (cond)
		{
		case R_ARROW:
			deltaT.x = c_deltaT;
			adjusting_tran = true;
			break;
		case L_ARROW:
			deltaT.x = -c_deltaT;
			adjusting_tran = true;
			break;
		case U_ARROW:
			deltaT.z = c_deltaT;
			adjusting_tran = true;
			break;
		case D_ARROW:
			deltaT.z = -c_deltaT;
			adjusting_tran = true;
			break;
		}
		if ((R_ARROW | ORI) == cond)
		{
			deltaR = c_deltaR;
			adjusting_rot = true;
		}
		else if ((L_ARROW | ORI) == cond)
		{
			deltaR = -c_deltaR;
			adjusting_rot = true;
		}

		if (adjusting_tran || adjusting_rot)
		{
			if (g_inst.DEF_MOCKSTEAM)
				Debug.LogWarning("SteamVR_ManagerDrv::actAdjustCar_u");
			else if(adjusting_tran)
				((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
			else if(adjusting_rot)
				((SteamVR_ManagerDrv)g_inst).adjustAvatar_r(deltaR);
			return true;
		}
		else
			return false;
	}

	protected static bool actAdjustCar_f(uint cond)
	{
		Vector3 deltaT = new Vector3(0, 0, 0);
		bool adjusting = false;
		switch(cond)
		{
		case L_ARROW:
			deltaT.x = c_deltaT;
			adjusting = true;
			break;
		case R_ARROW:
			deltaT.x = -c_deltaT;
			adjusting = true;
			break;
		case U_ARROW:
			deltaT.y = c_deltaT;
			adjusting = true;
			break;
		case D_ARROW:
			deltaT.y = -c_deltaT;
			adjusting = true;
			break;
		}
		if (adjusting)
		{
			if (g_inst.DEF_MOCKSTEAM)
				Debug.LogWarning("SteamVR_ManagerDrv::actAdjustCar_f");
			else
				((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
			return true;
		}
		else
			return false;
	}

	protected static bool actAvatarAdjF_m(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("SteamVR_ManagerDrv::actAvatarAdjF_m");
			return true;
		}
		else
		{
			Vector3 deltaT = new Vector3(0, 0, -c_deltaT);
			((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
			return true;
		}
	}

	protected static bool actAvatarAdjF_p(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("SteamVR_ManagerDrv::actAvatarAdjF_p");
			return true;
		}
		else
		{
			Vector3 deltaT = new Vector3(0, 0, c_deltaT);
			((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
			return true;
		}
	}

	protected static bool actAvatarAdjR_m(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("SteamVR_ManagerDrv::actAvatarAdjR_m");
			return true;
		}
		else
		{
			Vector3 deltaT = new Vector3(-c_deltaT, 0, 0);
			((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
			return true;
		}
	}

	protected static bool actAvatarAdjR_p(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("SteamVR_ManagerDrv::actAvatarAdjR_p");
			return true;
		}
		else
		{
			Vector3 deltaT = new Vector3(c_deltaT, 0, 0);
			((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
			return true;
		}
	}

	protected static bool actAvatarAdjU_m(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("SteamVR_ManagerDrv::actAvatarAdjU_m");
			return true;
		}
		else
		{
			Vector3 deltaT = new Vector3(0, -c_deltaT, 0);
			((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
			return true;
		}
	}

	protected static bool actAvatarAdjU_p(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("SteamVR_ManagerDrv::actAvatarAdjU_p");
			return true;
		}
		else
		{
			Vector3 deltaT = new Vector3(0, c_deltaT, 0);
			((SteamVR_ManagerDrv)g_inst).adjustAvatar_t(deltaT);
			return true;
		}
	}

	protected static bool actAvatarAdjO_m(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("SteamVR_ManagerDrv::actAvatarAdjO_m");
			return true;
		}
		else
		{
			((SteamVR_ManagerDrv)g_inst).adjustAvatar_r(-c_deltaR);
			return true;
		}
	}

	protected static bool actAvatarAdjO_p(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("SteamVR_ManagerDrv::actAvatarAdjO_p");
			return true;
		}
		else
		{
			((SteamVR_ManagerDrv)g_inst).adjustAvatar_r(c_deltaR);
			return true;
		}
	}

	protected override void UpdateInstructionDisplay(State s)
	{
		Debug.Assert(null != m_refDispHeader);
		m_refDispHeader.text = StateStringsDrv.s_shortDesc[(int)s];
		string body = StateStringsDrv.s_longDesc[(int)s] + "\n";
		for (int i_tran = 0; i_tran < m_transition.Length; i_tran ++)
		{
			string desc_tran = null;
			if (m_simplified)
				desc_tran = TransitionStringsDrvSimpl.s_Desc[i_tran];
			else
				desc_tran = TransitionStringsDrv.s_Desc[i_tran];
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

