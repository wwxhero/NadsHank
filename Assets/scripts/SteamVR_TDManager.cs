//====== Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Enables/disables objects based on connectivity and assigned roles.
//
//=============================================================================

using UnityEngine;
using Valve.VR;
public class SteamVR_TDManager : MonoBehaviour
{
	public GameObject m_hmd;
	public GameObject m_ctrlL, m_ctrlR;
	[Tooltip("Populate with objects you want to assign to additional controllers")]
	public GameObject[] m_objects;

	protected uint[] m_indicesDev; // assigned
	protected bool[] m_connected = new bool[OpenVR.k_unMaxTrackedDeviceCount]; // controllers only

	// cached roles - may or may not be connected
	protected uint m_ctrlLIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
	protected uint m_ctrlRIndex = OpenVR.k_unTrackedDeviceIndexInvalid;

	

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

	public SteamVR_TDManager()
	{
		inputFocusAction = SteamVR_Events.InputFocusAction(OnInputFocus);
		deviceConnectedAction = SteamVR_Events.DeviceConnectedAction(OnDeviceConnected);
		trackedDeviceRoleChangedAction = SteamVR_Events.SystemAction(EVREventType.VREvent_TrackedDeviceRoleChanged, OnTrackedDeviceRoleChanged);
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