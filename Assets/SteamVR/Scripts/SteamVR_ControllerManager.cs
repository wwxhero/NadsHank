﻿//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Enables/disables objects based on connectivity and assigned roles.
//
//=============================================================================

using UnityEngine;
using Valve.VR;

public class SteamVR_ControllerManager : MonoBehaviour
{
	public GameObject m_left, m_right;

	[Tooltip("Populate with objects you want to assign to additional controllers")]
	public GameObject[] m_objects;

	[Tooltip("Set to true if you want objects arbitrarily assigned to controllers before their role (left vs right) is identified")]
	public bool m_assignAllBeforeIdentified;

	uint[] m_indices; // assigned
	bool[] m_connected = new bool[OpenVR.k_unMaxTrackedDeviceCount]; // controllers only

	// cached roles - may or may not be connected
	uint m_leftIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
	uint m_rightIndex = OpenVR.k_unTrackedDeviceIndexInvalid;

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
		SetUniqueObject(m_right, 0);
		SetUniqueObject(m_left, 1);
		for (int i = 0; i < additional; i++)
			SetUniqueObject(objects[i], 2 + i);

		// Reset assignments.
		m_indices = new uint[2 + additional];
		for (int i = 0; i < m_indices.Length; i++)
			m_indices[i] = OpenVR.k_unTrackedDeviceIndexInvalid;
	}

	SteamVR_Events.Action inputFocusAction, deviceConnectedAction, trackedDeviceRoleChangedAction;

	void Awake()
	{
		UpdateTargets();
	}

	SteamVR_ControllerManager()
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

			m_indices[i] = OpenVR.k_unTrackedDeviceIndexInvalid;
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
				if (i != objectIndex && m_indices[i] == trackedDeviceIndex)
				{
					var obj = m_objects[i];
					if (obj != null)
						obj.SetActive(false);

					m_indices[i] = OpenVR.k_unTrackedDeviceIndexInvalid;
				}
			}
		}

		// Only set when changed.
		if (trackedDeviceIndex != m_indices[objectIndex])
		{
			m_indices[objectIndex] = trackedDeviceIndex;

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
			m_leftIndex = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
			m_rightIndex = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
		}

		// If neither role has been assigned yet, try hooking up at least the right controller.
		if (m_leftIndex == OpenVR.k_unTrackedDeviceIndexInvalid && m_rightIndex == OpenVR.k_unTrackedDeviceIndexInvalid)
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
			SetTrackedDeviceIndex(objectIndex++, (m_rightIndex < m_connected.Length && m_connected[m_rightIndex]) ? m_rightIndex : OpenVR.k_unTrackedDeviceIndexInvalid);
			SetTrackedDeviceIndex(objectIndex++, (m_leftIndex < m_connected.Length && m_connected[m_leftIndex]) ? m_leftIndex : OpenVR.k_unTrackedDeviceIndexInvalid);

			// Assign out any additional controllers only after both left and right have been assigned.
			if (m_leftIndex != OpenVR.k_unTrackedDeviceIndexInvalid && m_rightIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
			{
				for (uint deviceIndex = 0; deviceIndex < m_connected.Length; deviceIndex++)
				{
					if (objectIndex >= m_objects.Length)
						break;

					if (!m_connected[deviceIndex])
						continue;

					if (deviceIndex != m_leftIndex && deviceIndex != m_rightIndex)
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

