using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ExternalObjectsControlComLib;
using System.Xml;
using JointsReduction;

public class ScenarioControlPed : MonoBehaviour {

	private string m_scenePath;
	public GameObject[] m_vehiPrefabs;
	public GameObject m_pedPrefab;
	public GameObject m_drvPrefab;
	public GameObject m_mockTrackersPrefab;
	public bool m_bDriver;
	IDistriObjsCtrl m_ctrl;
	Dictionary<int, GameObject> m_id2Dyno = new Dictionary<int, GameObject>();
	Dictionary<int, GameObject> m_id2Ped = new Dictionary<int, GameObject>();
	GameObject m_mockTrackers;

	Matrix4x4 c_sim2unity;
	Matrix4x4 c_unity2sim;

	public bool DEF_LOGMATRIXFAC;
	public bool DEF_LOGJOINTTRAN;
	public bool DEF_LOGJOINTIDNAME;
	enum IMPLE { IGCOMM = 0, DISVRLINK };
	enum TERMINAL { edo_controller = 0, ado_controller, ped_controller };

	// Use this for initialization
	ScenarioControlPed()
	{
		Matrix4x4 m_2 = Matrix4x4.zero;
		m_2[0, 0] = 1;
		m_2[1, 2] = 1;
		m_2[2, 1] = 1;
		m_2[3, 3] = 1;
		//the matrix:
		//      1 0 0 0
		//      0 0 1 0
		//      0 1 0 0
		//      0 0 0 1

		Matrix4x4 m_1 = Matrix4x4.identity;
		m_1[0, 3] = -40920;
		m_1[1, 3] = -1320;
		m_1[2, 3] = 0;
		//the matrix:
		//      1 0 0 -40920
		//      0 1 0  -1320
		//      0 0 1      0
		//      0 0 0      1

		Matrix4x4 m_s_f2m = Matrix4x4.zero;
		float f2m = 0.3048f;
		m_s_f2m[0, 0] = f2m;
		m_s_f2m[1, 1] = f2m;
		m_s_f2m[2, 2] = f2m;
		m_s_f2m[3, 3] = 1;
		//the matrix:
		//      f2m 0   0   0
		//      0   f2m 0   0
		//      0   0   f2m 0
		//      0   0   0   1
		c_sim2unity =  m_s_f2m * m_2 * m_1;
		c_unity2sim = c_sim2unity.inverse;

	}



	void Start () {
		if (null == m_ctrl)
		{
			try
			{
				XmlDocument scene = new XmlDocument();
				scene.Load("SceneDistri.xml");
				XmlNode root = scene.DocumentElement;
				XmlAttribute attr_root = root.Attributes["path"];
				m_scenePath = attr_root.Value;
				m_ctrl = new DistriObjsCtrlClass();
				m_ctrl.CreateNetworkExternalObjectControl((int)IMPLE.DISVRLINK, (int)TERMINAL.ped_controller);
				m_ctrl.Initialize(m_scenePath);
			}
			catch (System.IO.FileNotFoundException)
			{
				Debug.Log("scene load failed!");
			}
			catch (Exception e)
			{
				Debug.Log(e.Message);
				m_ctrl.ReleaseNetworkExternalObjectControl();
				m_ctrl = null;
			}
		}
	}
	// clean up the code
	void OnDestroy()
	{
		if (null != m_ctrl)
		{
			m_ctrl.UnInitialize();
			m_ctrl.ReleaseNetworkExternalObjectControl();
			m_ctrl = null;
		}
	}
	// Update is called once per frame
	void Update () {
		if (null != m_ctrl)
		{
			try
			{
				m_ctrl.PreUpdateDynamicModels();
				List<KeyValuePair<int, int>> peg_pairs = null;
				EVT evt = EVT.evtUndefined;
				bool nonEvt = true;
				do
				{
					m_ctrl.QFrontEvent(out evt, out nonEvt);
					if (!nonEvt)
					{
						switch (evt)
						{
							case EVT.crtDyno:
								{
									int id;
									string name;
									int solId;
									double xSize, ySize, zSize;
									double xPos, yPos, zPos;
									double xTan, yTan, zTan;
									double xLat, yLat, zLat;
									m_ctrl.GetcrtDynoTuple(out id, out name, out solId
											, out xSize, out ySize, out zSize
											, out xPos, out yPos, out zPos
											, out xTan, out yTan, out zTan
											, out xLat, out yLat, out zLat);
									Vector3 p = new Vector3((float)xPos, (float)yPos, (float)zPos);
									Vector3 t = new Vector3((float)xTan, (float)yTan, (float)zTan);
									Vector3 l = new Vector3((float)xLat, (float)yLat, (float)zLat);
									Vector3 p_unity = c_sim2unity.MultiplyPoint3x4(p);
									Vector3 t_unity = MultiplyDir(c_sim2unity, t);
									Vector3 l_unity = MultiplyDir(c_sim2unity, l);
									Quaternion q_unity;
									FrameToQuaternionVehi(t_unity, l_unity, out q_unity);
									int idx = solId % m_vehiPrefabs.Length;
									GameObject o = Instantiate(m_vehiPrefabs[idx], p_unity, q_unity);
									o.name = name;
									m_id2Dyno.Add(id, o);
									break;
								}
							case EVT.delDyno:
								{
									int id;
									m_ctrl.GetdelDynoTuple(out id);
									GameObject o;
									if (m_id2Dyno.TryGetValue(id, out o))
									{
										m_id2Dyno.Remove(id);
										GameObject.Destroy(o);
									}
									break;
								}
							case EVT.crtPed:
								{
									int id;
									string name;
									int solId;
									double xSize, ySize, zSize;
									double xPos, yPos, zPos;
									double xTan, yTan, zTan;
									double xLat, yLat, zLat;
									int nParts;
									m_ctrl.GetcrtPedTuple(out id, out name, out solId
											, out xSize, out ySize, out zSize
											, out xPos, out yPos, out zPos
											, out xTan, out yTan, out zTan
											, out xLat, out yLat, out zLat
											, out nParts);
									Vector3 p = new Vector3((float)xPos, (float)yPos, (float)zPos);
									Vector3 t = new Vector3((float)xTan, (float)yTan, (float)zTan);
									Vector3 l = new Vector3((float)xLat, (float)yLat, (float)zLat);
									Vector3 p_unity = c_sim2unity.MultiplyPoint3x4(p);
									Vector3 t_unity = MultiplyDir(c_sim2unity, t);
									Vector3 l_unity = MultiplyDir(c_sim2unity, l);
									Quaternion q_unity;
									FrameToQuaternionPed(t_unity, l_unity, out q_unity);
									GameObject ped = null;
									bool own = (0 == id);
									if (m_bDriver && own)
										ped = Instantiate(m_drvPrefab, p_unity, q_unity);
									else
										ped = Instantiate(m_pedPrefab, p_unity, q_unity);
									ped.name = name;
									m_id2Ped.Add(id, ped);
									if (own)
									{
										ped.AddComponent<Manipulator>();
										RootMotion.FinalIK.VRIK ik = ped.AddComponent<RootMotion.FinalIK.VRIK>();
										ik.AutoDetectReferences();

										if (null != m_mockTrackersPrefab)
										{
											m_mockTrackers = Instantiate(m_mockTrackersPrefab, p_unity, q_unity);
											RootMotion.Demos.VRIKCalibrationController caliCtrl = GetComponent<RootMotion.Demos.VRIKCalibrationController>();
											caliCtrl.ik = ik;
											Transform[] trackers = { caliCtrl.headTracker
																, caliCtrl.bodyTracker
																, caliCtrl.leftHandTracker
																, caliCtrl.rightHandTracker
																, caliCtrl.leftFootTracker
																, caliCtrl.rightFootTracker
															};
											string[] targetNames = { "Pelvis/Spine1/Spine2/Spine3/Neck1/NeckHead/Tracker Mock (CenterEyeAnchor)"
																, "Pelvis/Tracker Mock (Body)"
																, "Pelvis/Spine1/Spine2/Spine3/LArmCollarbone/LArmUpper1/LArmUpper2/LArmForearm1/LArmForearm2/LArmHand/Tracker Mock (Left Hand)"
																, "Pelvis/Spine1/Spine2/Spine3/RArmCollarbone/RArmUpper1/RArmUpper2/RArmForearm1/RArmForearm2/RArmHand/Tracker Mock (Right Hand)"
																, "Pelvis/LLegUpper/LLegCalf/LLegAnkle/Tracker Mock (Left Foot)"
																, "Pelvis/RLegUpper/RLegCalf/RLegAnkle/Tracker Mock (Right Foot)"
															};
											caliCtrl.headTracker = m_mockTrackers.transform.Find(targetNames[0]);
											caliCtrl.bodyTracker = m_mockTrackers.transform.Find(targetNames[1]);
											caliCtrl.leftHandTracker = m_mockTrackers.transform.Find(targetNames[2]);
											caliCtrl.rightHandTracker = m_mockTrackers.transform.Find(targetNames[3]);
											caliCtrl.leftFootTracker = m_mockTrackers.transform.Find(targetNames[4]);
											caliCtrl.rightFootTracker = m_mockTrackers.transform.Find(targetNames[5]);
										}
										else if(!m_bDriver)
										{
											GameObject streamVR = GameObject.Find("Steam_VR_Activator_&_Avatar_Handler");
											Debug.Assert(null != streamVR);
											model_and_Steam_VR_Controller ctrl = streamVR.GetComponent<model_and_Steam_VR_Controller>();
											ctrl.pedestrian = ped;
										}

										if (DEF_LOGMATRIXFAC)
											ped.AddComponent<JointDumper>();
									}


									//bind joints with (id, name)
									int [] ids = new int[nParts];
									string [] names = new string[nParts];
									for (int i_part = 0; i_part < nParts; i_part ++)
									{
									   string namePartS;
									   m_ctrl.GetcrtPedPartName(id, i_part, out namePartS); //fixme: replace it with node name
									   if (DEF_LOGJOINTIDNAME)
									   {
										   string log = string.Format("{0}:{1}", i_part, namePartS);
										   Debug.Log(log);
									   }
									   ids[i_part] = i_part;
									   names[i_part] = namePartS;
									}
									DriverDiguy driver = ped.GetComponent<DriverDiguy>();
									driver.Initialize(ids, names, own);

									break;
								}
							case EVT.delPed:
								{
									int id;
									m_ctrl.GetdelPedTuple(out id);

									GameObject o;
									if (m_id2Ped.TryGetValue(id, out o))
									{
										m_id2Ped.Remove(id);
										GameObject.Destroy(o);
									}

									break;
								}
							case EVT.pegPed:
								{
                                    int id_parent, id_child;
                                    m_ctrl.GetpegPedTuple(out id_parent, out id_child);
                                    KeyValuePair<int, int> peg = new KeyValuePair<int, int>(id_parent, id_child);
                                    if (null == peg_pairs)
                                    	peg_pairs = new List<KeyValuePair<int, int>>();
                                    peg_pairs.Add(peg);
                                    break;
								}

						}
						m_ctrl.QPopEvent();
					}
				} while (!nonEvt);

				if (null != peg_pairs)
				{
					foreach (KeyValuePair<int, int> peg in peg_pairs)
					{
						int i_parent = peg.Key;
						int i_child = peg.Value;
						GameObject parent = null;
						bool hit = m_id2Dyno.TryGetValue(i_parent, out parent);
						Debug.Assert(hit);
						GameObject child = null;
						hit = m_id2Ped.TryGetValue(i_child, out child);
						Debug.Assert(hit);
						child.transform.parent = parent.transform;
						if (0 == i_child)
						{
							if (null != m_mockTrackers)
							{
								m_mockTrackers.transform.parent = parent.transform;
							}
							else
							{
								Debug.Assert(false, "adjust trackers in child space of pegging parent");
							}
						}
					}
				}

				GameObject pedOwn;
				const int c_ownPedId = 0;
				bool found = m_id2Ped.TryGetValue(c_ownPedId, out pedOwn);
				if (found)
				{
					Vector3 pos_unity = pedOwn.transform.position;
					Vector3 tan_unity = pedOwn.transform.forward;
					Vector3 lat_unity = pedOwn.transform.right;
					Vector3 p = c_unity2sim.MultiplyPoint3x4(pos_unity);
					Vector3 t = MultiplyDir(c_unity2sim, tan_unity);
					Vector3 l = MultiplyDir(c_unity2sim, lat_unity);
					double xPos, yPos, zPos;
					double xTan, yTan, zTan;
					double xLat, yLat, zLat;
					xPos = p.x; yPos = p.y; zPos = p.z;
					xTan = t.x; yTan = t.y; zTan = t.z;
					xLat = l.x; yLat = l.y; zLat = l.z;

					DriverDiguy driver = pedOwn.GetComponent<DriverDiguy>();
					driver.SyncOut();
					for (int i_part = 0; i_part < driver.m_art.Length; i_part ++)
					{
						ArtPart art = driver.m_art[i_part];
						m_ctrl.OnPushUpdateArt(c_ownPedId, art.id, art.q.w, art.q.x, art.q.y, art.q.z, art.t.x, art.t.y, art.t.z);
						//fixme: performance might be sacrified here from loop manage to native code call
					}

					m_ctrl.OnPostPushUpdateArt(c_ownPedId
										, xPos, yPos, zPos
										, xTan, yTan, zTan
										, xLat, yLat, zLat);


				}

				foreach (KeyValuePair<int, GameObject> kv in m_id2Dyno)
				{
					bool received = true;
					double xPos, yPos, zPos;
					double xTan, yTan, zTan;
					double xLat, yLat, zLat;
					m_ctrl.OnGetUpdate(kv.Key, out received
									, out xPos, out yPos, out zPos
									, out xTan, out yTan, out zTan
									, out xLat, out yLat, out zLat);
					if (received)
					{
						Vector3 p = new Vector3((float)xPos, (float)yPos, (float)zPos);
						Vector3 t = new Vector3((float)xTan, (float)yTan, (float)zTan);
						Vector3 l = new Vector3((float)xLat, (float)yLat, (float)zLat);
						Vector3 p_unity = c_sim2unity.MultiplyPoint3x4(p);
						Vector3 t_unity = MultiplyDir(c_sim2unity, t);
						Vector3 l_unity = MultiplyDir(c_sim2unity, l);
						Quaternion q_unity;
						FrameToQuaternionVehi(t_unity, l_unity, out q_unity);
						kv.Value.transform.position = p_unity;
						kv.Value.transform.rotation = q_unity;
					}
//fixme debugging log
					//string strTuple = string.Format("\nid = {10} received = {0}:\n\tpos=[{1},{2},{3}]\n\ttan=[{4},{5},{6}]\n\tlat=[{7},{8},{9}]"
					//                                    , received, xPos, yPos, zPos, xTan, yTan, zTan, xLat, yLat, zLat, kv.Key);
					//Debug.Log(strTuple);
				}

				foreach (KeyValuePair<int, GameObject> kv in m_id2Ped)
				{
					if (0 == kv.Key) //id(0) is own object
						continue;
					bool received = true;
					double xPos, yPos, zPos;
					double xTan, yTan, zTan;
					double xLat, yLat, zLat;
					m_ctrl.OnPreGetUpdateArt(kv.Key, out received
									, out xPos, out yPos, out zPos
									, out xTan, out yTan, out zTan
									, out xLat, out yLat, out zLat);
					if (received)
					{
						Vector3 p = new Vector3((float)xPos, (float)yPos, (float)zPos);
						Vector3 t = new Vector3((float)xTan, (float)yTan, (float)zTan);
						Vector3 l = new Vector3((float)xLat, (float)yLat, (float)zLat);
						Vector3 p_unity = c_sim2unity.MultiplyPoint3x4(p);
						Vector3 t_unity = MultiplyDir(c_sim2unity, t);
						Vector3 l_unity = MultiplyDir(c_sim2unity, l);
						Quaternion q_unity;
						FrameToQuaternionPed(t_unity, l_unity, out q_unity);
						kv.Value.transform.position = p_unity;
						kv.Value.transform.rotation = q_unity;
						DriverDiguy driver = kv.Value.GetComponent<DriverDiguy>();
						double q_w, q_x, q_y, q_z;
						double t_x, t_y, t_z;
						for (int i_part = 0; i_part < driver.m_art.Length; i_part ++)
						{
							//fixme: performance might be sacrified here from loop manage to native code call
							ArtPart art = driver.m_art[i_part];
							m_ctrl.OnGetUpdateArt(kv.Key, art.id
								, out q_w, out q_x, out q_y, out q_z
								, out t_x, out t_y, out t_z);
							art.q.Set((float)q_x, (float)q_y, (float)q_z, (float)q_w);
							art.t.Set((float)t_x, (float)t_y, (float)t_z);
						}
						driver.SyncIn();
					}
//fixme debugging log
					//string strTuple = string.Format("\nid = {10} received = {0}:\n\tpos=[{1},{2},{3}]\n\ttan=[{4},{5},{6}]\n\tlat=[{7},{8},{9}]"
					//                                    , received, xPos, yPos, zPos, xTan, yTan, zTan, xLat, yLat, zLat, kv.Key);
					//Debug.Log(strTuple);
				}

				m_ctrl.PostUpdateDynamicModels();
			}
			catch (Exception e)
			{
				Debug.Log(e.Message);
			}
		}
	}

	void FrameToQuaternionVehi(Vector3 t, Vector3 l, out Quaternion q)
	{
		Vector3 z_prime = Vector3.Cross(l, -t);
		Vector3 y_prime = -t;
		q = new Quaternion();
		q.SetLookRotation(z_prime, y_prime);
	}

	void FrameToQuaternionPed(Vector3 t, Vector3 l, out Quaternion q)
	{
		Vector3 y_prime = Vector3.Cross(t, l);
		Vector3 z_prime = t;
		q = new Quaternion();
		q.SetLookRotation(z_prime, y_prime);
	}



	void JointQuatU2D(Quaternion q_u, out double q_s_w, out double q_s_x, out double q_s_y, out double q_s_z, Matrix4x4 m)
	{
		q_s_w = q_u.w;
		Vector3 q_v_u = new Vector3(q_u.x, q_u.y, q_u.z);
		Vector3 q_v_s = -m.MultiplyVector(q_v_u);
		q_s_x = q_v_s.x;
		q_s_y = q_v_s.y;
		q_s_z = q_v_s.z;
	}

	void JointQuatD2U(double q_s_w, double q_s_x, double q_s_y, double q_s_z, out Quaternion q_u, Matrix4x4 m)
	{
		q_u.w = (float)q_s_w;
		Vector3 q_v_s = new Vector3((float)q_s_x, (float)q_s_y, (float)q_s_z);
		Vector3 q_v_u = -m.MultiplyVector(q_v_s);
		q_u.x = (float)q_v_u.x;
		q_u.y = (float)q_v_u.y;
		q_u.z = (float)q_v_u.z;
	}

	Vector3 MultiplyDir(Matrix4x4 m, Vector3 d)
	{
		Vector3 d_prime = m.MultiplyVector(d);
		d_prime.Normalize();
		return d_prime;
	}

	void JointVectorU2D(Vector3 vec_offset_u, out double v_x, out double v_y, out double v_z, Matrix4x4 m)
	{
		float epsilon_10 = 0.001f; //unit in meters
		if (vec_offset_u.x < epsilon_10 && vec_offset_u.x > -epsilon_10
			&& vec_offset_u.y < epsilon_10 && vec_offset_u.y > -epsilon_10
			&& vec_offset_u.z < epsilon_10 && vec_offset_u.z > -epsilon_10)
		{
			v_x = 0; v_y = 0; v_z = 0;
		}
		else
		{
			Vector3 vec_offset_d = m.MultiplyVector(vec_offset_u);
			v_x = vec_offset_d.x;
			v_y = vec_offset_d.y;
			v_z = vec_offset_d.z;
		}
	}

	void JointVectorD2U(double v_d_x, double v_d_y, double v_d_z, out Vector3 vec_offset_u, Matrix4x4 m)
	{
		Vector3 vec_offset_d = new Vector3((float)v_d_x, (float)v_d_y, (float)v_d_z);
		vec_offset_u = m.MultiplyVector(vec_offset_d);
	}

}
