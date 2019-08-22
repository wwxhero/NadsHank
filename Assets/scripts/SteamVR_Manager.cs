using UnityEngine;
using Valve.VR;
using RootMotion.FinalIK;
using System.Collections.Generic;
using UnityEngine.UI;

public class SteamVR_Manager : SteamVR_TDManager
{
	public bool DEF_MOCKSTEAM = true;
	public bool DEF_DBG = true;
	public bool DEF_TESTTELEPORT = true;
	public GameObject m_senarioCtrl;
	public GameObject m_prefMirror;
	public Text m_refDispHeader;
	public Text m_refDispBody;
	protected GameObject m_mirrow;
	bool m_trackersIdentified = false;
	[HideInInspector]
	public GameObject Avatar
	{
        get { return m_avatar; }
		set {
		    m_avatar = value;
		    gameObject.SetActive(true);
		}
	}
	protected GameObject m_avatar;
	protected VRIKCalibrator.CalibrationData m_data = new VRIKCalibrator.CalibrationData();

	protected int tracker_start, tracker_end;

	protected delegate bool Action(uint cond);
	protected enum State {
						  initial, pre_cnn, post_cnn, pre_calibra, post_calibra, tracking				//the common states shared for driver and pedestrain
						, teleporting																	//the specific state for pedestrain
						, pegging, pre_calibra2, pre_adjF, post_adjF, pre_adjR, post_adjR, pre_adjU, post_adjU, pre_adjO, post_adjO	//the specific state for driver
					};


	protected class Transition
	{
		public State From
		{
			get { return m_vec[0]; }
		}
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

	enum CtrlCode { trigger, steam, menu, pad_p, pad_t, grip, forward, right, up, ori, plus, minus, n_code };
	protected static uint R_TRIGGER = 	0x0001;
	protected static uint R_STEAM = 	0x0002;
	protected static uint R_MENU = 		0x0004;
	protected static uint R_PAD_P = 	0x0008;
	protected static uint R_PAD_T = 	0x0010;
	protected static uint R_GRIP = 		0x0020;
	protected static uint L_TRIGGER = 	0x0100;
	protected static uint L_STEAM = 	0x0200;
	protected static uint L_MENU = 		0x0400;
	protected static uint L_PAD_P = 	0x0800;
	protected static uint L_PAD_T = 	0x1000;
	protected static uint L_GRIP = 		0x2000;

	protected static uint FORWARD =	   0x10000;
	protected static uint RIGHT =	   0x20000;
	protected static uint UP =		   0x40000;
	protected static uint ORI =		   0x80000;
	protected static uint PLUS =	  0x100000;
	protected static uint MINUS =	  0x200000;
	protected static uint ALL =		0xffffffff;
	protected static uint NONE = 	0x00000000;
	protected Transition[] m_transition;
	protected static SteamVR_Manager g_inst;


	public SteamVR_Manager()
	{
		g_inst = this;
	}


    public virtual bool IdentifyTrackers()
	{
		return false;
	}

	protected static bool actIdentifyTrackers(uint cond)
	{
		return g_inst.m_trackersIdentified = g_inst.IdentifyTrackers();
	}

	static int s_idx = 0;
	protected static bool actTeleportP(uint cond)
	{
		if (g_inst.DEF_TESTTELEPORT)
		{
			ScenarioControl scenario = g_inst.m_senarioCtrl.GetComponent<ScenarioControl>();
			return scenario.testTeleport(++s_idx);
		}
		else
			return true;
	}

	protected static bool actTeleportM(uint cond)
	{
		if (g_inst.DEF_TESTTELEPORT)
		{
			ScenarioControl scenario = g_inst.m_senarioCtrl.GetComponent<ScenarioControl>();
			return scenario.testTeleport(--s_idx);
		}
		else
			return true;
	}

	public virtual void ConnectVirtualWorld()
	{
		Debug.Assert(false); //be override by derived class
	}
	protected static bool actConnectVirtualWorld(uint cond)
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

	protected static bool actUnConnectVirtualWorld(uint cond)
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

	public virtual void ShowMirror()
	{
		Debug.Assert(null == m_mirrow && null != m_avatar);
		m_mirrow = Instantiate(m_prefMirror);
		m_mirrow.transform.position = m_avatar.transform.position + 2f * m_avatar.transform.forward;
		m_mirrow.transform.rotation = m_avatar.transform.rotation;
	}


	protected static bool actShowMirror(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
			Debug.LogWarning("actShowMirror");
		else
			g_inst.ShowMirror();
		return true;
	}

	public virtual void HideMirror()
	{
		Debug.Assert(null != m_mirrow && null != m_avatar);
		GameObject.Destroy(m_mirrow);
		m_mirrow = null;
	}

	protected static bool actUnShowMirror(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
			Debug.LogWarning("actUnShowMirror");
		else
		{
			g_inst.HideMirror();
		}
		return true;
	}

	public virtual GameObject GetPrimeMirror()
	{
		return m_mirrow;
	}

	protected static bool actAdjustMirror(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("actAdjustMirror");
			return false;
		}
		else
		{
			GameObject mirror = g_inst.GetPrimeMirror();
			Debug.Assert(null != mirror);
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
				Vector3 tran = dz * mirror.transform.forward;
				mirror.transform.Translate(tran, Space.World);
			}
			return acted;
		}
	}

	protected static bool actAdjustAvatar(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("actAdjustAvatar");
			return false;
		}
		else
		{
			bool minus = (cond == L_MENU);
			bool plus = (cond == R_MENU);
			bool acted = (minus
						|| plus);
			float dh = 0;
			if (minus)
				dh = -0.001f;
			else if (plus)
				dh = 0.001f;
			if (acted)
			{
				Debug.Assert(g_inst.m_senarioCtrl);
				float s = g_inst.m_senarioCtrl.GetComponent<ScenarioControl>().adjustAvatar(dh);
				g_inst.adjustAvatar_s(s);
			}
			return acted;
		}
	}

	private	void adjustAvatar_s(float s)
	{
		Vector3 p = transform.worldToLocalMatrix.MultiplyPoint3x4(m_hmd.transform.position); p.y = 0;
		Vector3 t = p*(1-s);

		//through optimisation
		Matrix4x4 l2w = transform.localToWorldMatrix;
		Matrix4x4 w2p = (null == transform.parent) ? Matrix4x4.identity : transform.parent.worldToLocalMatrix;
		Matrix4x4 l2p = w2p*l2w;

		Vector3 s_prime = transform.localScale * s;
		Quaternion r_prime = transform.localRotation;
		Vector3 p_prime = l2p.MultiplyPoint3x4(t);
		transform.localPosition = p_prime;
		transform.localRotation = r_prime;
		transform.localScale = s_prime;

		float s_inv = 1/s;
		foreach (GameObject tracker in m_objects)
			tracker.transform.localScale *= s_inv;
	}

	protected void adjustAvatar_t(Vector3 t)
	{
		Vector3 t_w = m_avatar.transform.localToWorldMatrix.MultiplyVector(t);
		transform.Translate(t_w, Space.World);
	}

	protected void adjustAvatar_r(float d)
	{
		Vector3 p = m_avatar.transform.position;
		Vector3 axis = m_avatar.transform.up;
		transform.RotateAround(p, axis, d);
	}

	protected static bool actHideTracker(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("actHideTracker");
			return true;
		}
		else
		{
			for (int i_tracker = g_inst.tracker_start; i_tracker < g_inst.tracker_end; i_tracker++)
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

	protected static bool actUnHideTracker(uint cond)
	{
		if (g_inst.DEF_MOCKSTEAM)
		{
			Debug.LogWarning("actUnHideTracker");
			return true;
		}
		else
		{
			for (int i_tracker = g_inst.tracker_start; i_tracker < g_inst.tracker_end; i_tracker++)
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


	public virtual bool Calibration()
	{
		Debug.Assert(false); //override this function in derived class
		return false;
	}
	protected static bool actCalibration(uint cond)
	{
		Debug.Assert(null != g_inst
			&& null != g_inst.m_avatar);
		bool cali_done = true;
		if (g_inst.DEF_MOCKSTEAM)
			Debug.LogWarning("actCalibration");
		else
		{
			cali_done = g_inst.Calibration();
			if (cali_done)
			{
				GameObject eyeCam = g_inst.m_hmd.transform.parent.gameObject;
				Camera cam = eyeCam.GetComponent<Camera>();
				Debug.Assert(null != cam);
				cam.nearClipPlane = 0.1f;
			}

		}
		return cali_done;
	}
	public virtual void UnCalibration()
	{
		Debug.Assert(false); //override this funciton in derived class
	}

	protected static bool actUnCalibration(uint cond)
	{
		Debug.Assert(null != g_inst
			&& null != g_inst.m_avatar);
		if (g_inst.DEF_MOCKSTEAM)
			Debug.LogWarning("actUnCalibration");
		else
		{
			g_inst.UnCalibration();
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
		bool[] ctrl_switch = new bool[] {
									  Input.GetKey(KeyCode.T) && Input.GetKey(KeyCode.RightShift)
									, Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.RightShift)
									, Input.GetKey(KeyCode.M) && Input.GetKey(KeyCode.RightShift)
									, Input.GetKey(KeyCode.P) && Input.GetKey(KeyCode.RightShift)
									, Input.GetKey(KeyCode.O) && Input.GetKey(KeyCode.RightShift)
									, Input.GetKey(KeyCode.G) && Input.GetKey(KeyCode.RightShift)
									, Input.GetKey(KeyCode.T) && Input.GetKey(KeyCode.LeftShift)
									, Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.LeftShift)
									, Input.GetKey(KeyCode.M) && Input.GetKey(KeyCode.LeftShift)
									, Input.GetKey(KeyCode.P) && Input.GetKey(KeyCode.LeftShift)
									, Input.GetKey(KeyCode.O) && Input.GetKey(KeyCode.LeftShift)
									, Input.GetKey(KeyCode.G) && Input.GetKey(KeyCode.LeftShift)
									, Input.GetKey(KeyCode.F) //&& (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
									, Input.GetKey(KeyCode.R) //&& (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
									, Input.GetKey(KeyCode.U) //&& (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
									, Input.GetKey(KeyCode.O) //&& (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
									, Input.GetKey(KeyCode.RightArrow)
									, Input.GetKey(KeyCode.LeftArrow)
								};
		uint[] switch_codes = new uint[] {
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
									, FORWARD
									, RIGHT
									, UP
									, ORI
									, PLUS
									, MINUS
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
		if (s_np != s_n)
			UpdateInstructionDisplay(m_state);
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
				, "FRONT"
				, "RIGHT"
				, "UP"
				, "ROTATION"
				, "RIGHT_ARROW"
				, "LEFT_ARROW"
			};
			Debug.Assert(switch_names.Length == ctrl_switch.Length);
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
	protected virtual void UpdateInstructionDisplay(State s)
	{
		Debug.Assert(false); //this function need to be override by a derived class
	}
	public void Transport(Quaternion r, Vector3 t)
	{
		//fixme: a smooth transit should happen for transport
		transform.position = t;
		transform.rotation = r;
	}

	public override void Refresh()
	{
		if (!m_trackersIdentified)
			base.Refresh();
	}

	protected static bool actAdjustAvatarInspec_r(uint cond)
	{
		ScenarioControl sc = g_inst.m_senarioCtrl.GetComponent<ScenarioControl>();
		sc.adjustInspector(ScenarioControl.InspectorHelper.Direction.right, false);
		return true;
	}

	protected static bool actAdjustAvatarInspec_u(uint cond)
	{
		ScenarioControl sc = g_inst.m_senarioCtrl.GetComponent<ScenarioControl>();
		sc.adjustInspector(ScenarioControl.InspectorHelper.Direction.up, false);
		return true;
	}

	protected static bool actAdjustAvatarInspec_f(uint cond)
	{
		ScenarioControl sc = g_inst.m_senarioCtrl.GetComponent<ScenarioControl>();
		sc.adjustInspector(ScenarioControl.InspectorHelper.Direction.forward, false);
		return true;
	}
}