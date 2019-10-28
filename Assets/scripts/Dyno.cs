using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dyno : MonoBehaviour {

	Vector3 m_posW;
	Quaternion m_rotW;

	public void Lock(bool a_lock)
	{
		if (a_lock)
		{
			transform.position = m_posW;
			transform.rotation = m_rotW;
		}
	}

	public void SetDft_w(Vector3 pos, Quaternion rot)
	{
		m_posW = pos;
		m_rotW = rot;
	}

	public void TranslateDft_w(Vector3 tran_w)
	{
		m_posW = m_posW + tran_w;
	}
}
