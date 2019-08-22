using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MockPhysics : MonoBehaviour {
	public enum Mount {head = 0, body, lh, rh, lf, rf, total};
	public Transform [] m_trackersMt;
	public Transform m_leftShoulder;
	public Transform m_rightShoulder;
}
