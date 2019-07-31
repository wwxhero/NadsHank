//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: For controlling in-game objects with tracked devices.
//
//=============================================================================

using UnityEngine;
using Valve.VR;

public class SteamVR_TrackedObject : MonoBehaviour
{
	public enum EIndex
	{
		None = -1,
		Hmd = (int)OpenVR.k_unTrackedDeviceIndex_Hmd,
		Device1,
		Device2,
		Device3,
		Device4,
		Device5,
		Device6,
		Device7,
		Device8,
		Device9,
		Device10,
		Device11,
		Device12,
		Device13,
		Device14,
		Device15
	}

	public EIndex index;

	[Tooltip("If not set, relative to parent")]
	public Transform origin;

	public bool isValid { get; private set; }

	private bool m_lock = false;
	public bool Lock(bool l)
	{
		bool lock_prev = m_lock;
		m_lock = l;
		return lock_prev;
	}
	private Vector3 m_posDft = new Vector3(0, 0, 0);
    private Quaternion m_rotDft = Quaternion.identity;
	public void SetDft(Vector3 p, Quaternion r)
	{
		m_posDft = p;
		m_rotDft = r;
	}
	public void Lock(Vector3 p, Quaternion r)
	{
		m_posDft = p;
		m_rotDft = r;
		m_lock = true;
	}

	private void OnNewPoses(TrackedDevicePose_t[] poses)
	{
		if (m_lock)
		{
			if (origin != null)
			{
				transform.position = origin.transform.TransformPoint(m_posDft);
				transform.rotation = origin.rotation * m_rotDft;
			}
			else
			{
				transform.localPosition = m_posDft;
				transform.localRotation = m_rotDft;
			}
		}

		if (index == EIndex.None
			|| m_lock)
			return;

		var i = (int)index;

		isValid = false;
		if (poses.Length <= i)
			return;

		if (!poses[i].bDeviceIsConnected)
			return;

		if (!poses[i].bPoseIsValid)
			return;

		isValid = true;

		var pose = new SteamVR_Utils.RigidTransform(poses[i].mDeviceToAbsoluteTracking);



		if (origin != null)
		{
			transform.position = origin.transform.TransformPoint(pose.pos);
			transform.rotation = origin.rotation * pose.rot;
		}
		else
		{
			transform.localPosition = pose.pos;
			transform.localRotation = pose.rot;
		}
	}

	SteamVR_Events.Action newPosesAction;

	SteamVR_TrackedObject()
	{
		newPosesAction = SteamVR_Events.NewPosesAction(OnNewPoses);
	}

	void OnEnable()
	{
		var render = SteamVR_Render.instance;
		if (render == null)
		{
			enabled = false;
			return;
		}

		newPosesAction.enabled = true;
	}

	void OnDisable()
	{
		newPosesAction.enabled = false;
		isValid = false;
	}

	public void SetDeviceIndex(int index)
	{
		if (System.Enum.IsDefined(typeof(EIndex), index))
			this.index = (EIndex)index;
	}
}

