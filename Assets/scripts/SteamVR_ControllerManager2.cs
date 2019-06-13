//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Enables/disables objects based on connectivity and assigned roles.
//
//=============================================================================

using UnityEngine;
using Valve.VR;
using RootMotion.FinalIK;

public class SteamVR_ControllerManager2 : MonoBehaviour
{
	public bool DEF_DBG = true;
	public GameObject m_avatar;
	public GameObject m_hmd;
	public GameObject m_ctrlL, m_ctrlR;

	[Tooltip("Populate with objects you want to assign to additional controllers")]
	public GameObject[] m_objects;
	enum ObjType {tracker_rfoot=2, tracker_lfoot, tracker_pelvis, tracker_rhand, tracker_lhand};

	[Tooltip("Set to true if you want objects arbitrarily assigned to controllers before their role (left vs right) is identified")]
	public bool m_assignAllBeforeIdentified;

	uint[] m_indicesDev; // assigned
	bool[] m_connected = new bool[OpenVR.k_unMaxTrackedDeviceCount]; // controllers only

	// cached roles - may or may not be connected
	uint m_ctrlLIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
	uint m_ctrlRIndex = OpenVR.k_unTrackedDeviceIndexInvalid;

	enum State {initial, unidentified, identified, calibrating, tracking, teleporting, adjusting };
	bool [] m_donecali = new bool[6] {false, false, false, false, false, false};
	enum CaliPart {head = 0, rfoot, lfoot, pelvis, rhand, lhand};
	VRIKCalibrator.CalibrationData m_data = new VRIKCalibrator.CalibrationData();
	State m_state = State.initial;

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
	}

	void Update()
	{
		State s_n = m_state;
		bool ctrls_ready = (m_ctrlRIndex != OpenVR.k_unTrackedDeviceIndexInvalid
						&& m_ctrlLIndex != OpenVR.k_unTrackedDeviceIndexInvalid);
		bool stemtrackers_ready = (OpenVR.k_unTrackedDeviceIndexInvalid != m_indicesDev[(int)ObjType.tracker_pelvis]
								&& OpenVR.k_unTrackedDeviceIndexInvalid != m_indicesDev[(int)ObjType.tracker_lfoot]
								&& OpenVR.k_unTrackedDeviceIndexInvalid != m_indicesDev[(int)ObjType.tracker_rfoot]);
		bool handstrackers_ready =(OpenVR.k_unTrackedDeviceIndexInvalid != m_indicesDev[(int)ObjType.tracker_lhand]
								&& OpenVR.k_unTrackedDeviceIndexInvalid != m_indicesDev[(int)ObjType.tracker_rhand]);
		GameObject[] ctrls = {m_ctrlR, m_ctrlL};
		bool [] trigger = {false, false};
		bool [] gripped = {false, false};
		for (int i_ctrl = 0; i_ctrl < ctrls.Length; i_ctrl ++)
		{
			if (null != ctrls[i_ctrl])
			{
				SteamVR_TrackedController ctrl = ctrls[i_ctrl].GetComponent<SteamVR_TrackedController>();
				Debug.Assert(null != ctrl);
				trigger[i_ctrl] = ctrl.triggerPressed;
				gripped[i_ctrl] = ctrl.gripped;
			}
		}
		bool stateChanged = false;
		if (State.initial == m_state
			&& ctrls_ready)
		{
			if (Identify())
				m_state = State.identified;
			else
				m_state = State.unidentified;
		}
		stateChanged = (m_state != s_n);

		if ((State.identified == m_state || State.calibrating == m_state)
			&& !stateChanged)
		{
			if ((gripped[0] && gripped[1])
				&& null != m_avatar)
			{
				ConnectVirtualWorld();
				m_state = State.calibrating; //fixme: this code is for testing VW connection only
			}
		}
		stateChanged = (m_state != s_n);

		if (State.calibrating == m_state
				&& (trigger[0] || trigger[1])
				&& !stateChanged)
		{
			m_data = VRIKCalibrator2.Calibrate(m_avatar.GetComponent<VRIK>()
				, m_hmd.transform
				, m_objects[(int)ObjType.tracker_pelvis].transform
				, m_objects[(int)ObjType.tracker_lhand].transform
				, m_objects[(int)ObjType.tracker_rhand].transform
				, m_objects[(int)ObjType.tracker_lfoot].transform
				, m_objects[(int)ObjType.tracker_rfoot].transform);
		}



		State s_np = m_state;
		if (DEF_DBG)
		{
			string strInfo = string.Format("state transition:{0}=>{1}", s_n.ToString(), s_np.ToString());
			Debug.Log(strInfo);
		}
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

	public void Refresh()
	{
		int objectIndex = 0;

		var system = OpenVR.System;
		if (system != null)
		{
			m_ctrlLIndex = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
			m_ctrlRIndex = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
		}

		// If neither role has been assigned yet, try hooking up at least the right controller.
		if (m_ctrlLIndex == OpenVR.k_unTrackedDeviceIndexInvalid && m_ctrlRIndex == OpenVR.k_unTrackedDeviceIndexInvalid)
		{
			for (uint deviceIndex = 0; deviceIndex < m_connected.Length; deviceIndex++)
			{
				if (objectIndex >= m_objects.Length)
					break;

				if (!m_connected[deviceIndex])
					continue;

				SetTrackedDeviceIndex(objectIndex++, deviceIndex);

				if (!m_assignAllBeforeIdentified)
					break;
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

	private bool Identify()
	{
		return true;
	}

	private void ConnectVirtualWorld()
	{
		Matrix4x4 v2p = transform.worldToLocalMatrix;
		Vector3 t_p = v2p.MultiplyVector(m_hmd.transform.forward);
		Vector3 u_p = v2p.MultiplyVector(m_hmd.transform.up);
		Vector3 r_p = Vector3.Cross(u_p, t_p);
		Matrix4x4 m_p = new Matrix4x4(new Vector4(t_p.x, t_p.y, t_p.z, 0f)
									, new Vector4(u_p.x, u_p.y, u_p.z, 0f)
									, new Vector4(r_p.x, r_p.y, r_p.z, 0f)
									, new Vector4(0f, 0f, 0f, 1f));

		Transform v = m_avatar.transform;
		Vector3 t_v = v.forward;
		Vector3 u_v = v.up;
		Vector3 r_v = v.right;
		Matrix4x4 m_v = new Matrix4x4(new Vector4(t_v.x, t_v.y, t_v.z, 0f)
									, new Vector4(u_v.x, u_v.y, u_v.z, 0f)
									, new Vector4(r_v.x, r_v.y, r_v.z, 0f)
									, new Vector4(0f, 0f, 0f, 1f));

		Matrix4x4 l = m_v.transpose * m_p;

		//Vector3 o_p = (m_ctrlL.transform.localPosition + m_ctrlR.transform.localPosition) * 0.5f; //fixme: orientation in tracking space should be decided in more sophisticated way
		Vector3 o_p = (m_objects[(int)ObjType.tracker_lfoot].transform.localPosition + m_objects[(int)ObjType.tracker_rfoot].transform.localPosition) * 0.5f; //fixme: orientation in tracking space should be decided in more sophisticated way
		o_p.y = 0.0f;
		Vector3 o_v = v.position;
		Vector3 t = -l.MultiplyVector(o_p) + o_v;

		Transport(l.rotation, t);
	}

	private void Transport(Quaternion r, Vector3 t)
	{
		//fixme: a smooth transit should happen for transport
		transform.position = t;
		transform.rotation = r;
	}
}

