//====== Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Enables/disables objects based on connectivity and assigned roles.
//
//=============================================================================

using UnityEngine;
using Valve.VR;
using RootMotion.FinalIK;
using System.Collections.Generic;

public class SteamVR_ControllerManager2 : MonoBehaviour
{
	public bool DEF_MOCKSTEAM = true;
	public bool DEF_DBG = true;

	public GameObject m_senarioCtrl;
	public GameObject m_prefMirror;
	private GameObject m_mirrow;
	[HideInInspector]
	public GameObject m_avatar;
	public GameObject m_hmd;
	public GameObject m_ctrlL, m_ctrlR;

	[Tooltip("Populate with objects you want to assign to additional controllers")]
	public GameObject[] m_objects;
	enum ObjType { tracker_start = 2, tracker_rfoot = tracker_start, tracker_lfoot, tracker_pelvis, tracker_rhand, tracker_lhand, tracker_end };

	uint[] m_indicesDev; // assigned
	bool[] m_connected = new bool[OpenVR.k_unMaxTrackedDeviceCount]; // controllers only

	// cached roles - may or may not be connected
	uint m_ctrlLIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
	uint m_ctrlRIndex = OpenVR.k_unTrackedDeviceIndexInvalid;

	VRIKCalibrator.CalibrationData m_data = new VRIKCalibrator.CalibrationData();

	delegate bool Action(uint cond);
	enum State { initial, pre_transport, post_transport, pre_calibra, post_calibra, tracking };
	class Transition
	{
		private State[] m_vec = new State[2];
		private Action[] m_acts;
		private uint m_cond = 0;
		public Transition(State from, State to, uint cond)
		{
			m_vec[0] = from;
			m_vec[1] = to;
			m_cond = cond;
		}
		public Transition(State from, State to, uint cond, Action act)
		{
			m_vec[0] = from;
			m_vec[1] = to;
			m_cond = cond;
			m_acts = new Action[1] { act };
		}
		public Transition(State from, State to, uint cond, Action[] acts)
		{
			m_vec[0] = from;
			m_vec[1] = to;
			m_cond = cond;
			m_acts = acts;
		}
		public bool Exe(ref State cur, uint cond)
		{
			bool hit = (cur == m_vec[0]
						&& (m_cond == ALL || m_cond == cond));
			bool executed = false;
			if (hit)
			{
				executed = true;
				if (null != m_acts)
				{
					for (int i_act = 0; i_act < m_acts.Length && executed; i_act++)
						executed = executed && m_acts[i_act](cond);
				}
				if (executed)
					cur = m_vec[1];
			}
			return executed;
		}
	};

	enum CtrlCode { trigger, steam, menu, pad_p, pad_t, grip, n_code };
	static uint R_TRIGGER = 0x0001;
	static uint R_STEAM = 0x0002;
	static uint R_MENU = 0x0004;
	static uint R_PAD_P = 0x0008;
	static uint R_PAD_T = 0x0010;
	static uint R_GRIP = 0x0020;
	static uint L_TRIGGER = 0x0100;
	static uint L_STEAM = 0x0200;
	static uint L_MENU = 0x0400;
	static uint L_PAD_P = 0x0800;
	static uint L_PAD_T = 0x1000;
	static uint L_GRIP = 0x2000;
	static uint ALL = 0xffffffff;
	Transition[] m_transition = new Transition[] {
									  new Transition(State.initial, State.pre_transport, ALL)
									, new Transition(State.pre_transport, State.post_transport, R_TRIGGER, new Action[] {actIdentifyTrackers, actConnectVirtualWorld})
									, new Transition(State.pre_transport, State.post_transport, L_TRIGGER, new Action[] {actIdentifyTrackers, actConnectVirtualWorld})
									, new Transition(State.post_transport, State.pre_transport, L_GRIP, actUnConnectVirtualWorld)
									, new Transition(State.post_transport, State.pre_calibra, R_GRIP, actShowMirror)
									, new Transition(State.pre_calibra, State.pre_calibra, ALL, actAdjustMirror)
									, new Transition(State.pre_calibra, State.post_calibra, R_TRIGGER, actCalibration)
									, new Transition(State.pre_calibra, State.post_calibra, L_TRIGGER, actCalibration)
									, new Transition(State.post_calibra, State.post_calibra, ALL, actAdjustMirror)
									, new Transition(State.post_calibra, State.tracking, R_GRIP, new Action[]{ actUnShowMirror, actHideTracker })
									, new Transition(State.post_calibra, State.pre_calibra, L_GRIP, actUnCalibration)
									, new Transition(State.post_calibra, State.pre_transport, L_MENU|R_MENU, new Action[]{actUnShowMirror, actUnCalibration, actUnConnectVirtualWorld})
									, new Transition(State.tracking, State.pre_transport, L_MENU|R_MENU, new Action[]{actUnHideTracker, actUnCalibration, actUnConnectVirtualWorld})
									, new Transition(State.tracking, State.tracking, R_GRIP, actTeleportP)
									, new Transition(State.tracking, State.tracking, L_GRIP, actTeleportM)
								};
	static SteamVR_ControllerManager2 g_inst;

	class Tracker
	{
		GameObject tracker;
		float r, u;
		int r_d, u_d;
		Tracker(GameObject a_tracker, float a_r, float a_u)
		{
			tracker = a_tracker;
			r = a_r;
			u = a_u;
		}

		static int Compare_r(Tracker x, Tracker y)
		{
			float d = x.r - y.r;
			if (d < 0)
				return -1;
			else if (d > 0)
				return +1;
			else
				return 0;
		}

		static int Compare_u(Tracker x, Tracker y)
		{
			float d = x.u - y.u;
			if (d < 0)
				return -1;
			else if (d > 0)
				return +1;
			else
				return 0;
		}

		static bool IsRightFoot(Tracker t)
		{
			return (0 == t.u_d || 1 == t.u_d)
				&& (3 == t.r_d || 4 == t.r_d);
		}

		static bool IsLeftFoot(Tracker t)
		{
			return (0 == t.u_d || 1 == t.u_d)
				&& (0 == t.r_d || 1 == t.r_d);
		}

		static bool IsPelvis(Tracker t)
		{
			return 2 == t.u_d && 2 == t.r_d;
		}

		static bool IsRightHand(Tracker t)
		{
			return (3 == t.u_d || 4 == t.u_d)
				&& (3 == t.r_d || 4 == t.r_d);
		}

		static bool IsLeftHand(Tracker t)
		{
			return (3 == t.u_d || 4 == t.u_d)
				&& (0 == t.r_d || 1 == t.r_d);
		}
		delegate bool Predicate(Tracker t);
		public static bool IdentifyTrackers(GameObject[] a_trackers, Transform a_hmd)
		{
			Debug.Assert(a_trackers.Length == 5); //supports 5 trackers only
			if (5 != a_trackers.Length)
				return false;
			Tracker[] trackers = new Tracker[5];
			List<Tracker> lst_r = new List<Tracker>();
			List<Tracker> lst_u = new List<Tracker>();
			for (int i_tracker = 0; i_tracker < 5; i_tracker++)
			{
				GameObject o_t = a_trackers[i_tracker];
				if (!o_t.activeSelf)
					return false;
				Vector3 v_t = o_t.transform.position - a_hmd.position;
				float r_t = Vector3.Dot(a_hmd.right, v_t);
				float u_t = Vector3.Dot(a_hmd.up, v_t);
				Tracker t = new Tracker(o_t, r_t, u_t);
				trackers[i_tracker] = t;
				lst_r.Add(t);
				lst_u.Add(t);
			}
			lst_r.Sort(Tracker.Compare_r);
			lst_u.Sort(Tracker.Compare_u);
			List<Tracker>.Enumerator it = lst_r.GetEnumerator();
			bool next = it.MoveNext();
			for (int i_r = 0
				; next && i_r < trackers.Length
				; i_r++, next = it.MoveNext())
			{
				Tracker t = it.Current;
				t.r_d = i_r;
			}

			it = lst_u.GetEnumerator();
			next = it.MoveNext();
			for (int i_u = 0
				; next && i_u < trackers.Length
				; i_u++, next = it.MoveNext())
			{
				Tracker t = it.Current;
				t.u_d = i_u;
			}

			Tracker.Predicate[] predicates = new Tracker.Predicate[] {
				Tracker.IsRightFoot, Tracker.IsLeftFoot, Tracker.IsPelvis, Tracker.IsRightHand, Tracker.IsLeftHand
			};

			int[] hit_trackers = new int[] {
				-1, -1, -1, -1, -1
			};

			for (int i_tracker = 0; i_tracker < trackers.Length; i_tracker++)
			{
				bool identified = false;
				Tracker t = trackers[i_tracker];
				int id = 0;
				while (id < predicates.Length)
				{
					identified = predicates[id](t);
					if (identified)
						break;
					else
						id++;
				}
				if (!identified)
					break;
				hit_trackers[id] = i_tracker;

			}

			if (hit_trackers[0] > -1
			 && hit_trackers[1] > -1
			 && hit_trackers[2] > -1
			 && hit_trackers[3] > -1
			 && hit_trackers[4] > -1)
			{
				a_trackers[0] = trackers[hit_trackers[0]].tracker;
				a_trackers[1] = trackers[hit_trackers[1]].tracker;
				a_trackers[2] = trackers[hit_trackers[2]].tracker;
				a_trackers[3] = trackers[hit_trackers[3]].tracker;
				a_trackers[4] = trackers[hit_trackers[4]].tracker;
				return true;
			}
			else
				return false;
		}
	};
	private static bool actIdentifyTrackers(uint cond)
	{
		Transform ori = g_inst.m_hmd.transform;
		GameObject [] trackers = new GameObject[5] {
			  g_inst.m_objects[(int)ObjType.tracker_rfoot]
			, g_inst.m_objects[(int)ObjType.tracker_lfoot]
			, g_inst.m_objects[(int)ObjType.tracker_pelvis]
			, g_inst.m_objects[(int)ObjType.tracker_rhand]
			, g_inst.m_objects[(int)ObjType.tracker_lhand]
		};
		if (Tracker.IdentifyTrackers(trackers, ori))
		{
			g_inst.m_objects[(int)ObjType.tracker_rfoot] = trackers[0];
			g_inst.m_objects[(int)ObjType.tracker_lfoot] = trackers[1];
			g_inst.m_objects[(int)ObjType.tracker_pelvis] = trackers[2];
			g_inst.m_objects[(int)ObjType.tracker_rhand] = trackers[3];
			g_inst.m_objects[(int)ObjType.tracker_lhand] = trackers[4];
			return true;
		}
		else
			return false;
	}

	static int s_idx = 0;
	private static bool actTeleportP(uint cond)
	{
		if (g_inst.DEF_MOCKTRANSPORT)
		{
			ScenarioControlPed scenario = g_inst.m_senarioCtrl.GetComponent<ScenarioControlPed>();
			return scenario.testTeleport(++s_idx);
		}
		else
			return true;
	}

	private static bool actTeleportM(uint cond)
	{
		if (g_inst.DEF_MOCKTRANSPORT)
		{
			ScenarioControlPed scenario = g_inst.m_senarioCtrl.GetComponent<ScenarioControlPed>();
			return scenario.testTeleport(--s_idx);
		}
		else
			return true;
	}

	private static bool actConnectVirtualWorld(uint cond)
	{
		Debug.Assert(null != g_inst
			&& null != g_inst.m_avatar);
		if (g_inst.DEF_MOCKSTEAM)
			Debug.LogWarning("actConnectVirtualWorld");
		else
		{
			if (g_inst)
				g_inst.ConnectVirtualWorld();
		}
		return true;
	}

	private static bool actUnConnectVirtualWorld(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
			Debug.LogWarning("actUnConnectVirtualWorld");
		else
		{
			Quaternion q = new Quaternion(0, 0, 0, 1);
			Vector3 p = new Vector3(0, 0, 0);
			g_inst.Transport(q, p);
		}
		return true;
	}

	private static bool actShowMirror(uint cond)
	{
		//fixme: a mirror is supposed to show at a right position
		if (g_inst.DEF_MOCKSTEAM)
			Debug.LogWarning("actShowMirror");
		else
		{
			Debug.Assert(null == g_inst.m_mirrow && null != g_inst.m_avatar);
			g_inst.m_mirrow = Instantiate(g_inst.m_prefMirror);
			g_inst.m_mirrow.transform.position = g_inst.m_avatar.transform.position + 2f * g_inst.m_avatar.transform.forward;
			g_inst.m_mirrow.transform.rotation = g_inst.m_avatar.transform.rotation;
		}
		return true;
	}

	private static bool actUnShowMirror(uint cond)
	{
		//fixme: a mirror is supposed to show at a right position
		if (g_inst.DEF_MOCKSTEAM)
			Debug.LogWarning("actUnShowMirror");
		else
		{
			Debug.Assert(null != g_inst.m_mirrow && null != g_inst.m_avatar);
			GameObject.Destroy(g_inst.m_mirrow);
			g_inst.m_mirrow = null;
		}
		return true;
	}

	private static bool actAdjustMirror(uint cond)
	{
		//fixme: adjust the mirror with the ctrl code
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("actAdjustMirror");
			return false;
		}
		else
		{
			Debug.Assert(null != g_inst.m_mirrow);
			bool l_pad_t = (cond == L_PAD_T);
			bool r_pad_t = (cond == R_PAD_T);
			bool acted = (l_pad_t
						|| r_pad_t);
			float dz = 0;
			if (l_pad_t)
				dz = 0.01f;
			else if (r_pad_t)
				dz = -0.01f;
			if (acted)
			{
				Vector3 tran = dz * g_inst.m_mirrow.transform.forward;
				g_inst.m_mirrow.transform.Translate(tran);
			}
			return acted;
		}
	}

	private static bool actHideTracker(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("actHideTracker");
			return true;
		}
		else
		{
			for (int i_tracker = (int)ObjType.tracker_start; i_tracker < (int)ObjType.tracker_end; i_tracker++)
			{
				GameObject tracker = g_inst.m_objects[i_tracker];
				foreach (Transform sub_t in tracker.transform)
				{
					SteamVR_RenderModel render = sub_t.gameObject.GetComponent<SteamVR_RenderModel>();
					if (null != render)
						sub_t.gameObject.SetActive(false);
				}
			}
			return true;
		}
	}

	private static bool actUnHideTracker(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("actUnHideTracker");
			return true;
		}
		else
		{
			for (int i_tracker = (int)ObjType.tracker_rfoot; i_tracker < (int)ObjType.tracker_end; i_tracker++)
			{
				GameObject tracker = g_inst.m_objects[i_tracker];
				foreach (Transform sub_t in tracker.transform)
				{
					SteamVR_RenderModel render = sub_t.gameObject.GetComponent<SteamVR_RenderModel>();
					if (null != render)
						sub_t.gameObject.SetActive(true);
				}
			}
			return true;
		}
	}



	private static bool actCalibration(uint cond)
	{
		Debug.Assert(null != g_inst
			&& null != g_inst.m_avatar);
		if (g_inst.DEF_MOCKSTEAM)
			Debug.LogWarning("actCalibration");
		else
		{
			if (null != g_inst
				&& null != g_inst.m_avatar)
			{
				VRIK ik = g_inst.m_avatar.GetComponent<VRIK>();
				g_inst.m_data = VRIKCalibrator2.Calibrate(ik, g_inst.m_hmd.transform
							, g_inst.m_objects[(int)ObjType.tracker_pelvis].transform
							, g_inst.m_objects[(int)ObjType.tracker_lhand].transform
							, g_inst.m_objects[(int)ObjType.tracker_rhand].transform
							, g_inst.m_objects[(int)ObjType.tracker_lfoot].transform
							, g_inst.m_objects[(int)ObjType.tracker_rfoot].transform);
			}
			GameObject eyeCam = g_inst.m_hmd.transform.parent.gameObject;
			Camera cam = eyeCam.GetComponent<Camera>();
			Debug.Assert(null != cam);
			cam.nearClipPlane = 0.1f;
		}
		return true;
	}

	private static bool actUnCalibration(uint cond)
	{
		Debug.Assert(null != g_inst
			&& null != g_inst.m_avatar);
		if (g_inst.DEF_MOCKSTEAM)
			Debug.LogWarning("actUnCalibration");
		else
		{
			if (null != g_inst
				&& null != g_inst.m_avatar)
			{
				VRIK ik = g_inst.m_avatar.GetComponent<VRIK>();
				VRIKCalibrator2.UnCalibrate(ik, g_inst.m_hmd.transform
							, g_inst.m_objects[(int)ObjType.tracker_pelvis].transform
							, g_inst.m_objects[(int)ObjType.tracker_lhand].transform
							, g_inst.m_objects[(int)ObjType.tracker_rhand].transform
							, g_inst.m_objects[(int)ObjType.tracker_lfoot].transform
							, g_inst.m_objects[(int)ObjType.tracker_rfoot].transform);
			}
			GameObject eyeCam = g_inst.m_hmd.transform.parent.gameObject;
			Camera cam = eyeCam.GetComponent<Camera>();
			Debug.Assert(null != cam);
			cam.nearClipPlane = 0.05f;
		}
		return true;
	}



	State m_state = State.initial;

	void Update()
	{
		State s_n = m_state;
		bool ctrls_ready = (m_ctrlRIndex != OpenVR.k_unTrackedDeviceIndexInvalid
						&& m_ctrlLIndex != OpenVR.k_unTrackedDeviceIndexInvalid);
		uint code_ctrl = 0x0;
		bool[] ctrl_switch = new bool[2 * (int)CtrlCode.n_code] {
									  Input.GetKeyDown(KeyCode.T) && Input.GetKey(KeyCode.RightShift)
									, Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.RightShift)
									, Input.GetKeyDown(KeyCode.M) && Input.GetKey(KeyCode.RightShift)
									, Input.GetKeyDown(KeyCode.P) && Input.GetKey(KeyCode.RightShift)
									, Input.GetKeyDown(KeyCode.O) && Input.GetKey(KeyCode.RightShift)
									, Input.GetKeyDown(KeyCode.G) && Input.GetKey(KeyCode.RightShift)
									, Input.GetKeyDown(KeyCode.T) && Input.GetKey(KeyCode.LeftShift)
									, Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftShift)
									, Input.GetKeyDown(KeyCode.M) && Input.GetKey(KeyCode.LeftShift)
									, Input.GetKeyDown(KeyCode.P) && Input.GetKey(KeyCode.LeftShift)
									, Input.GetKeyDown(KeyCode.O) && Input.GetKey(KeyCode.LeftShift)
									, Input.GetKeyDown(KeyCode.G) && Input.GetKey(KeyCode.LeftShift)
								};
		uint[] switch_codes = new uint[2 * (int)CtrlCode.n_code] {
									  R_TRIGGER
									, R_STEAM
									, R_MENU
									, R_PAD_P
									, R_PAD_T
									, R_GRIP
									, L_TRIGGER
									, L_STEAM
									, L_MENU
									, L_PAD_P
									, L_PAD_T
									, L_GRIP
								};

		if (ctrls_ready)
		{
			SteamVR_TrackedController ctrlR = m_ctrlR.GetComponent<SteamVR_TrackedController>();
			SteamVR_TrackedController ctrlL = m_ctrlL.GetComponent<SteamVR_TrackedController>();
			ctrl_switch[0] = ctrl_switch[0] || ctrlR.triggerPressed;
			ctrl_switch[1] = ctrl_switch[1] || ctrlR.steamPressed;
			ctrl_switch[2] = ctrl_switch[2] || ctrlR.menuPressed;
			ctrl_switch[3] = ctrl_switch[3] || ctrlR.padPressed;
			ctrl_switch[4] = ctrl_switch[4] || ctrlR.padTouched;
			ctrl_switch[5] = ctrl_switch[5] || ctrlR.gripped;
			ctrl_switch[6] = ctrl_switch[6] || ctrlL.triggerPressed;
			ctrl_switch[7] = ctrl_switch[7] || ctrlL.steamPressed;
			ctrl_switch[8] = ctrl_switch[8] || ctrlL.menuPressed;
			ctrl_switch[9] = ctrl_switch[9] || ctrlL.padPressed;
			ctrl_switch[10] = ctrl_switch[10] || ctrlL.padTouched;
			ctrl_switch[11] = ctrl_switch[11] || ctrlL.gripped;
		}

		for (int i_switch = 0; i_switch < ctrl_switch.Length; i_switch++)
		{
			if (ctrl_switch[i_switch])
				code_ctrl |= switch_codes[i_switch];
		}


		bool state_tran = false;
		int n_transi = m_transition.Length;
		for (int i_transi = 0; i_transi < n_transi && !state_tran; i_transi++)
			state_tran = m_transition[i_transi].Exe(ref m_state, code_ctrl);

		State s_np = m_state;
		if (DEF_DBG)
		{
			string switches = null;
			bool switched = false;
			string[] switch_names = {
				  "R_TRIGGER"
				, "R_STEAM"
				, "R_MENU"
				, "R_PAD_P"
				, "R_PAD_T"
				, "R_GRIP"
				, "L_TRIGGER"
				, "L_STEAM"
				, "L_MENU"
				, "L_PAD_P"
				, "L_PAD_T"
				, "L_GRIP"
			};
			for (int i_switch = 0; i_switch < ctrl_switch.Length; i_switch++)
			{
				switches += string.Format("{0}={1}\t", switch_names[i_switch], ctrl_switch[i_switch].ToString());
				switched = switched || ctrl_switch[i_switch];
			}
			if (switched)
				Debug.LogWarning(switches);
			string strInfo = string.Format("state transition:{0}=>{1}", s_n.ToString(), s_np.ToString());
			Debug.Log(strInfo);
		}
	}



	private void ConnectVirtualWorld()
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

	public void Transport(Quaternion r, Vector3 t)
	{
		//fixme: a smooth transit should happen for transport
		transform.position = t;
		transform.rotation = r;
	}
	// Helper function to avoid adding duplicates to object array.
	void SetUniqueObject(GameObject o, int index)
	{
		for (int i = 0; i < index; i++)
			if (m_objects[i] == o)
				return;

		m_objects[index] = o;
	}

	// This needs to be called if you update left, right or objects at runtime (e.g. when dyanmically spawned).
	public void UpdateTargets()
	{
		// Add left and right entries to the head of the list so we only have to operate on the list itself.
		var objects = m_objects;
		var additional = (objects != null) ? objects.Length : 0;
		m_objects = new GameObject[2 + additional];
		SetUniqueObject(m_ctrlR, 0);
		SetUniqueObject(m_ctrlL, 1);
		for (int i = 0; i < additional; i++)
			SetUniqueObject(objects[i], 2 + i);

		// Reset assignments.
		m_indicesDev = new uint[2 + additional];
		for (int i = 0; i < m_indicesDev.Length; i++)
			m_indicesDev[i] = OpenVR.k_unTrackedDeviceIndexInvalid;
	}

	SteamVR_Events.Action inputFocusAction, deviceConnectedAction, trackedDeviceRoleChangedAction;

	void Awake()
	{
		UpdateTargets();
	}

	SteamVR_ControllerManager2()
	{
		inputFocusAction = SteamVR_Events.InputFocusAction(OnInputFocus);
		deviceConnectedAction = SteamVR_Events.DeviceConnectedAction(OnDeviceConnected);
		trackedDeviceRoleChangedAction = SteamVR_Events.SystemAction(EVREventType.VREvent_TrackedDeviceRoleChanged, OnTrackedDeviceRoleChanged);
		g_inst = this;
	}

	void OnEnable()
	{
		for (int i = 0; i < m_objects.Length; i++)
		{
			var obj = m_objects[i];
			if (obj != null)
				obj.SetActive(false);

			m_indicesDev[i] = OpenVR.k_unTrackedDeviceIndexInvalid;
		}

		Refresh();

		for (int i = 0; i < SteamVR.connected.Length; i++)
			if (SteamVR.connected[i])
				OnDeviceConnected(i, true);

		inputFocusAction.enabled = true;
		deviceConnectedAction.enabled = true;
		trackedDeviceRoleChangedAction.enabled = true;
	}

	void OnDisable()
	{
		inputFocusAction.enabled = false;
		deviceConnectedAction.enabled = false;
		trackedDeviceRoleChangedAction.enabled = false;
	}

	static string hiddenPrefix = "hidden (", hiddenPostfix = ")";
	static string[] labels = { "left", "right" };

	// Hide controllers when the dashboard is up.
	private void OnInputFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			for (int i = 0; i < m_objects.Length; i++)
			{
				var obj = m_objects[i];
				if (obj != null)
				{
					var label = (i < 2) ? labels[i] : (i - 1).ToString();
					ShowObject(obj.transform, hiddenPrefix + label + hiddenPostfix);
				}
			}
		}
		else
		{
			for (int i = 0; i < m_objects.Length; i++)
			{
				var obj = m_objects[i];
				if (obj != null)
				{
					var label = (i < 2) ? labels[i] : (i - 1).ToString();
					HideObject(obj.transform, hiddenPrefix + label + hiddenPostfix);
				}
			}
		}
	}

	// Reparents to a new object and deactivates that object (this allows
	// us to call SetActive in OnDeviceConnected independently.
	private void HideObject(Transform t, string name)
	{
		if (t.gameObject.name.StartsWith(hiddenPrefix))
		{
			Debug.Log("Ignoring double-hide.");
			return;
		}
		var hidden = new GameObject(name).transform;
		hidden.parent = t.parent;
		t.parent = hidden;
		hidden.gameObject.SetActive(false);
	}
	private void ShowObject(Transform t, string name)
	{
		var hidden = t.parent;
		if (hidden.gameObject.name != name)
			return;
		t.parent = hidden.parent;
		Destroy(hidden.gameObject);
	}

	private void SetTrackedDeviceIndex(int objectIndex, uint trackedDeviceIndex)
	{
		//object index 0 and 1 are reserved for controllers
		Debug.Assert(objectIndex > 1
					|| OpenVR.k_unTrackedDeviceIndexInvalid == trackedDeviceIndex
					|| OpenVR.System.GetTrackedDeviceClass((uint)trackedDeviceIndex) == ETrackedDeviceClass.Controller);
		// First make sure no one else is already using this index.
		if (trackedDeviceIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
		{
			for (int i = 0; i < m_objects.Length; i++)
			{
				if (i != objectIndex && m_indicesDev[i] == trackedDeviceIndex)
				{
					var obj = m_objects[i];
					if (obj != null)
						obj.SetActive(false);

					m_indicesDev[i] = OpenVR.k_unTrackedDeviceIndexInvalid;
				}
			}
		}

		// Only set when changed.
		if (trackedDeviceIndex != m_indicesDev[objectIndex])
		{
			m_indicesDev[objectIndex] = trackedDeviceIndex;

			var obj = m_objects[objectIndex];
			if (obj != null)
			{
				if (trackedDeviceIndex == OpenVR.k_unTrackedDeviceIndexInvalid)
					obj.SetActive(false);
				else
				{
					obj.SetActive(true);
					obj.BroadcastMessage("SetDeviceIndex", (int)trackedDeviceIndex, SendMessageOptions.DontRequireReceiver);
				}
			}
		}
	}

	// Keep track of assigned roles.
	private void OnTrackedDeviceRoleChanged(VREvent_t vrEvent)
	{
		Refresh();
	}

	// Keep track of connected controller indices.
	private void OnDeviceConnected(int index, bool connected)
	{
		bool changed = m_connected[index];
		m_connected[index] = false;

		if (connected)
		{
			var system = OpenVR.System;
			if (system != null)
			{
				var deviceClass = system.GetTrackedDeviceClass((uint)index);
				if (deviceClass == ETrackedDeviceClass.Controller ||
					deviceClass == ETrackedDeviceClass.GenericTracker)
				{
					m_connected[index] = true;
					changed = !changed; // if we clear and set the same index, nothing has changed
				}
			}
		}

		if (changed)
			Refresh();
	}

	private uint GetControllerIndex(ETrackedControllerRole role)
	{
		uint i_dev = OpenVR.System.GetTrackedDeviceIndexForControllerRole(role);
		if (OpenVR.k_unTrackedDeviceIndexInvalid == i_dev)
			return OpenVR.k_unTrackedDeviceIndexInvalid;

		var error = ETrackedPropertyError.TrackedProp_Success;
		var capacity = OpenVR.System.GetStringTrackedDeviceProperty((uint)i_dev, ETrackedDeviceProperty.Prop_RenderModelName_String, null, 0, ref error);
		if (capacity <= 1)
		{
			Debug.LogError("Failed to get render model name for tracked object " + i_dev);
			return OpenVR.k_unTrackedDeviceIndexInvalid;
		}

		var buffer = new System.Text.StringBuilder((int)capacity);
		OpenVR.System.GetStringTrackedDeviceProperty((uint)i_dev, ETrackedDeviceProperty.Prop_RenderModelName_String, buffer, capacity, ref error);

		var s = buffer.ToString();
		if (s.Contains("tracker")) //messed up, controller turns to be a tracker
			return OpenVR.k_unTrackedDeviceIndexInvalid;
		else
			return i_dev;
	}

	public void Refresh()
	{
		int objectIndex = 0;

		var system = OpenVR.System;
		if (system != null)
		{
			m_ctrlLIndex = GetControllerIndex(ETrackedControllerRole.LeftHand);
			m_ctrlRIndex = GetControllerIndex(ETrackedControllerRole.RightHand);
		}

		// we need both controllers to be enabled
		if (m_ctrlLIndex == OpenVR.k_unTrackedDeviceIndexInvalid || m_ctrlRIndex == OpenVR.k_unTrackedDeviceIndexInvalid)
		{
			SetTrackedDeviceIndex(objectIndex++, OpenVR.k_unTrackedDeviceIndexInvalid);
			SetTrackedDeviceIndex(objectIndex++, OpenVR.k_unTrackedDeviceIndexInvalid);
			for (uint deviceIndex = 0; deviceIndex < m_connected.Length; deviceIndex++)
			{
				if (objectIndex >= m_objects.Length)
					break;

				if (!m_connected[deviceIndex])
					continue;

				SetTrackedDeviceIndex(objectIndex++, deviceIndex);
			}
		}
		else
		{
			SetTrackedDeviceIndex(objectIndex++, (m_ctrlRIndex < m_connected.Length && m_connected[m_ctrlRIndex]) ? m_ctrlRIndex : OpenVR.k_unTrackedDeviceIndexInvalid);
			SetTrackedDeviceIndex(objectIndex++, (m_ctrlLIndex < m_connected.Length && m_connected[m_ctrlLIndex]) ? m_ctrlLIndex : OpenVR.k_unTrackedDeviceIndexInvalid);

			// Assign out any additional controllers only after both left and right have been assigned.
			if (m_ctrlLIndex != OpenVR.k_unTrackedDeviceIndexInvalid && m_ctrlRIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
			{
				for (uint deviceIndex = 0; deviceIndex < m_connected.Length; deviceIndex++)
				{
					if (objectIndex >= m_objects.Length)
						break;

					if (!m_connected[deviceIndex])
						continue;

					if (deviceIndex != m_ctrlLIndex && deviceIndex != m_ctrlRIndex)
					{
						SetTrackedDeviceIndex(objectIndex++, deviceIndex);
					}
				}
			}
		}

		// Reset the rest.
		while (objectIndex < m_objects.Length)
		{
			SetTrackedDeviceIndex(objectIndex++, OpenVR.k_unTrackedDeviceIndexInvalid);
		}
	}




}

