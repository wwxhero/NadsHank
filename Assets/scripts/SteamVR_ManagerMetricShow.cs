using UnityEngine;
using Valve.VR;
public class SteamVR_ManagerMetricShow : SteamVR_TDManager
{
	void OnGUI()
	{
		string strInfo = null;
		for (int i_o = 0; i_o < m_objects.Length; i_o ++)
		{
			GameObject o = m_objects[i_o];
			Transform t = o.transform;
			strInfo += string.Format("\n{0}:[{1,6:#.0000}, {2,6:#.0000}, {3,6:#.0000}]", o.name, t.localPosition.x, t.localPosition.y, t.localPosition.z);
		}
		Rect rcTxt = new Rect(0, 0, Screen.width, Screen.height);
		GUI.Label(rcTxt, strInfo);
	}
}