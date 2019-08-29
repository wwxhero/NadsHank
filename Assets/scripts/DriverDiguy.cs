using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JointsReduction;

public class DriverDiguy : MonoBehaviour {
	private const int DFN_NJDIGUY = 15;
	public bool logging = false;
	public ArtPart[] m_art = new ArtPart[DFN_NJDIGUY];
	public string [] m_oriJs;
	public float [] m_disloc;
	//		= new string[DFN_NJDIGUY] {
	//				  "Armature"
	//				, "mixamorig:Hips"
	//				, "mixamorig:Spine1"
	//				, "mixamorig:LeftUpLeg"
	//				, "mixamorig:RightUpLeg"
	//				, "mixamorig:Neck"
	//				, "mixamorig:LeftArm"
	//				, "mixamorig:RightArm"
	//				, "mixamorig:LeftLeg"
	//				, "mixamorig:RightLeg"
	//				, "mixamorig:LeftForeArm"
	//				, "mixamorig:RightForeArm"
	//				, "mixamorig:LeftFoot"
	//				, "mixamorig:RightFoot"
	//				, "mixamorig:LeftHand"
	//				, "mixamorig:RightHand"
	//		};
	JointsMapDiGuy m_diguyMap = new JointsMapDiGuy();
	bool m_isDriving = true;
	public void Initialize(int [] ids, string [] names, bool isDriving)
	{
		Debug.Assert(ids.Length == DFN_NJDIGUY
					&& names.Length == DFN_NJDIGUY);
		Dictionary<string, ArtPart> name2art = new Dictionary<string, ArtPart>();
		for (int i_art = 0; i_art < DFN_NJDIGUY; i_art ++)
		{
			string name = names[i_art];
			ArtPart art = new ArtPart(ids[i_art], name, m_disloc[i_art]);
			m_art[i_art] = art;
			name2art[name] = art;
		}
		m_diguyMap.Initialize(transform, m_oriJs, name2art);
		m_isDriving = isDriving;
	}

	public void SyncOut()
	{
		m_diguyMap.mapOut();
	}

	public void SyncIn()
	{
		m_diguyMap.mapIn();
	}

	void Update()
	{
		if (logging)
		{
			string logInfo = null;
			for (int i = 0; i < m_art.Length; i++)
			{
				int id = m_art[i].id;
				string name = m_art[i].name;
				Vector3 trans = m_art[i].t;
				Vector3 euler = m_art[i].q.eulerAngles;
				float[] k = new float[3] { (180 - euler.x) / 360, (180 - euler.y) / 360, (180 - euler.z) / 360 };
				int[] k_prime = new int[3];
				for (int t = 0; t < 3; t++)
				{
					k_prime[t] = k[t] > 0 ? (int)k[t] : (int)k[t] - 1;
				}
				int[] a = new int[3] { (int)euler.x + 360 * k_prime[0], (int)euler.y + 360 * k_prime[1], (int)euler.z + 360 * k_prime[2] };
				logInfo += string.Format("\n{0,2},{1, 12}\t\t:[{2,6:#.00}\t{3,6:#.00}\t{4,6:#.00}]\t\t[{5,5:#.00}\t{6,5:#.00}\t{7,5:#.00}]",
										id, name, a[0], a[1], a[2], trans.x, trans.y, trans.z);
			}
			Debug.Log(logInfo);
		}
	}

}
