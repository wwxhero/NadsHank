using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ExternalObjectsControlComLib;
using System.Xml;
using JointsReduction;
using System.Globalization;

public class ScenarioControl : MonoBehaviour {

	private string m_scenePath;
	public GameObject[] m_vehiPrefabs;
	public GameObject m_pedPrefab;
	public GameObject m_drvPrefab;
	public GameObject m_camInspectorPrefab;
	public GameObject m_mockTrackersPrefab;
	public bool m_bDriver;
	IDistriObjsCtrl m_ctrl;
	Dictionary<int, GameObject> m_id2Dyno = new Dictionary<int, GameObject>();
	Dictionary<int, GameObject> m_id2Ped = new Dictionary<int, GameObject>();
	GameObject m_trackers;
	Camera m_egoInspector;

	Matrix4x4 c_sim2unity;
	Matrix4x4 c_unity2sim;

	public bool DEF_LOGMATRIXFAC;
	public bool DEF_LOGJOINTIDNAME;
	public bool DEF_TESTTELEPORT;
	public bool DEF_LOGPEDTRANSIM;

	enum IMPLE { IGCOMM = 0, DISVRLINK };
	enum TERMINAL { edo_controller = 0, ado_controller, ped_controller };
	enum LAYER {scene_static = 8, peer_dynamic, host_dynamic, ego_dynamic};
	public class ConfAvatar
	{
		public float Height
		{
			get { return height; }
		}
		public float Width
		{
			get { return width + (2*hand0) * width/width0; }
		}
		private float height;
		private float width;
		private float perceptualHeight;
		private const float height0 = 1.78f;
		private const float width0 = 1.40f;
		private const float hand0 = 0.25f;
		private Vector3 posTel, tanTel, latTel;
		private ArrayList lstPos_test = new ArrayList();
		private ArrayList lstTan_test = new ArrayList();
		private ArrayList lstLat_test = new ArrayList();
		public ConfAvatar(uint a_height, uint a_width)
		{
			height = ((float)a_height) * 0.01f;
			width = ((float)a_width) * 0.01f;
			perceptualHeight = height;
		}

		public float ScaleInv(float dh)
		{
			float h_prime = perceptualHeight + dh;
			float s_h_inv = perceptualHeight / h_prime;
			perceptualHeight = h_prime;
			return s_h_inv;
		}

		public void Apply(RootMotion.FinalIK.VRIK ped)
		{
			float s_h = height / height0;
			float s_w = width / (width0 * s_h);
			ped.references.root.localScale = new Vector3(s_h, s_h, s_h);
			ped.references.leftShoulder.localScale = new Vector3(1f, s_w, 1f);
			ped.references.rightShoulder.localScale = new Vector3(1f, s_w, 1f);
		}

		public void setTeleport(Vector3 p_sim, Vector3 t_sim, Vector3 l_sim)
		{
			posTel = p_sim; tanTel = t_sim; latTel = l_sim;
			Vector3 u = new Vector3(0, 0, 1);
			Vector3 l = Vector3.Cross(t_sim, u);
			Debug.Assert(Vector3.Dot(l, l_sim) > 1 - 0.01
						&& Vector3.Dot(l, l_sim) < 1 + 0.01);
		}

		public void getTeleport(out Vector3 p_sim, out Vector3 t_sim, out Vector3 l_sim)
		{
			p_sim = new Vector3(posTel.x, posTel.y, posTel.z);
			t_sim = new Vector3(tanTel.x, tanTel.y, tanTel.z);
			l_sim = new Vector3(latTel.x, latTel.y, latTel.z);
		}

		public void testTeleport(int idx, out Vector3 pos, out Vector3 tan, out Vector3 lat)
		{
			int i = idx % lstLat_test.Count;
			if (i < 0)
				i += lstLat_test.Count;
			Debug.Assert(!(i<0));
			pos = (Vector3)lstPos_test[i];
			tan = (Vector3)lstTan_test[i];
			lat = (Vector3)lstLat_test[i];
		}

		public void testAddTeleport(Vector3 p_sim, Vector3 t_sim, Vector3 l_sim)
		{
			lstPos_test.Add(p_sim);
			lstTan_test.Add(t_sim);
			lstLat_test.Add(l_sim);
		}


	};

	public class ConfVehical
	{

	};

	public class InspectorHelper
	{
		struct BBOX
		{
			public Vector3 center;
			public float s_x, s_y, s_z;
		};
		BBOX m_bbox;
		bool m_bHost;
		Transform m_target;
		public InspectorHelper(Transform target, ConfAvatar conf)
		{
			//fixme: intialize inspector helper for the avatar target
			m_bHost = false;
			m_target = target;
			m_bbox.center = new Vector3(0, conf.Height * 0.5f, 0);
			m_bbox.s_x = conf.Width * 0.5f;
			m_bbox.s_y = conf.Height * 0.5f;
			m_bbox.s_z = 0f; //fixme: the avatar is as thin as a paper
		}

		public InspectorHelper(Transform target, ConfVehical conf)
		{
			//fixme: intialize inspector helper for the vechical target
		}
		public enum Direction {forward = 0, up, right};

		public void Apply(Camera cam, Direction dir)
		{
			//fixme: put camera in the specific direction of the target
			float[] camSize = {
				  Mathf.Max(m_bbox.s_x, m_bbox.s_y)
				, Mathf.Max(m_bbox.s_x, m_bbox.s_z)
				, Mathf.Max(m_bbox.s_y, m_bbox.s_z)
			};
			int host_mask = 1 << (int)LAYER.host_dynamic;
			int ego_mask = 1 << (int)LAYER.ego_dynamic;
			cam.cullingMask = m_bHost ? host_mask|ego_mask : ego_mask;
			cam.orthographic = true;
			cam.orthographicSize = camSize[(int)dir];

			cam.transform.parent = m_target;
			const float c_distance = 10;
			Vector3 [] t_l = {
				  new Vector3(0, 0, 1)
				, new Vector3(0, 1, 0)
				, new Vector3(1, 0, 0)
			};
			Quaternion r_l = Quaternion.LookRotation(-t_l[(int)dir]);
			Vector3 p_l = t_l[(int)dir] * c_distance + m_bbox.center;
			cam.transform.localPosition = p_l;
			cam.transform.localRotation = r_l;

		}
	};

	ConfAvatar m_confAvatar;
	ConfVehical m_confVehicle;

	// Use this for initialization
	ScenarioControl()
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

	public bool testTeleport(int idx)
	{
		Vector3 pos, tan, lat;
		m_confAvatar.testTeleport(idx, out pos, out tan, out lat);
		Teleport(pos, tan, lat);
		return true;
	}

	void Teleport(Vector3 pos_s, Vector3 tan_s, Vector3 lat_s)
	{
		Vector3 t, l, p, u;
		Vector3 t_prime, l_prime, p_prime, u_prime;
		m_confAvatar.getTeleport(out p, out t, out l);
		t_prime = tan_s; l_prime = lat_s;p_prime = pos_s;
		m_confAvatar.setTeleport(p_prime, t_prime, l_prime);
		u = Vector3.Cross(t, l); u_prime = Vector3.Cross(t_prime, l_prime);
		Matrix4x4 m = new Matrix4x4(  new Vector4(t.x, t.y, t.z, 0)
									, new Vector4(l.x, l.y, l.z, 0)
									, new Vector4(u.x, u.y, u.z, 0)
									, new Vector4(p.x, p.y, p.z, 1));
		Matrix4x4 m_prime = new Matrix4x4(new Vector4(t_prime.x, t_prime.y, t_prime.z, 0)
										, new Vector4(l_prime.x, l_prime.y, l_prime.z, 0)
										, new Vector4(u_prime.x, u_prime.y, u_prime.z, 0)
										, new Vector4(p_prime.x, p_prime.y, p_prime.z, 1));
		Matrix4x4 t_s = m_prime * m.inverse;
		Matrix4x4 t_u = c_sim2unity * t_s * c_unity2sim;

		if (DEF_TESTTELEPORT)
		{
			GameObject ped = m_id2Ped[0];
			Debug.Assert(null != ped);
			string logStr = string.Format("tran_s:\n{0}\ntran_u:\n{1}", t_s.ToString(), t_u.ToString());
			Vector3 p_rig = ped.transform.position;
			p_rig = t_u.MultiplyPoint3x4(p_rig);
			Quaternion q_rig = ped.transform.rotation;
			q_rig = q_rig * t_u.rotation;
			ped.transform.position = p_rig;
			ped.transform.rotation = q_rig;
			Debug.LogWarning(logStr);
		}
		else
		{
			GameObject rigCam = GameObject.Find("[CameraRig]");
			Vector3 p_rig = rigCam.transform.position;
			p_rig = t_u.MultiplyPoint3x4(p_rig);
			Quaternion q_rig = rigCam.transform.rotation;
			q_rig = q_rig * t_u.rotation;
			SteamVR_Manager rigCamMgr = rigCam.GetComponent<SteamVR_Manager>();
			rigCamMgr.Transport(q_rig, p_rig);
		}

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

				XmlNodeList children = root.ChildNodes;
				for (int i_node = 0; i_node < children.Count; i_node ++)
				{
					XmlNode n_child = children[i_node];
					if ("avatar" == n_child.Name)
					{
						XmlElement e_avatar = (XmlElement)n_child;
						XmlAttribute height_attr = e_avatar.GetAttributeNode("height");
						XmlAttribute width_attr = e_avatar.GetAttributeNode("width");
						uint height = uint.Parse(height_attr.Value);
						uint width = uint.Parse(width_attr.Value);
						m_confAvatar = new ConfAvatar(height, width);
						XmlNodeList teleports = n_child.ChildNodes;
						if (null != teleports)
						{
							for (int i_tel = 0; i_tel < teleports.Count; i_tel ++)
							{
								XmlNode tel = teleports[i_tel];
								if ("teleport" != tel.Name)
									continue;
								XmlElement telElement = (XmlElement)tel;
								string [] name = {"x", "y", "z", "i", "j", "k"};
								float [] val  = new float[name.Length];
								for (int i_attr = 0; i_attr < name.Length; i_attr ++)
								{
									XmlElement v_text = telElement[name[i_attr]];
									val[i_attr] = float.Parse(v_text.InnerXml);
								}
								Vector3 u = new Vector3(0, 0, 1);
								Vector3 p = new Vector3(val[0], val[1], val[2]);
								Vector3 t = new Vector3(val[3], val[4], val[5]);
								Vector3 l = Vector3.Cross(t, u); //fixme: not yet confirmed the cross product order
								m_confAvatar.testAddTeleport(p, t, l);
							}
						}
					}
					else if("map" == n_child.Name)
					{
						XmlElement e_map = (XmlElement)n_child;
						XmlAttribute e_t_attr = e_map.GetAttributeNode("elevation_t");
						float e_t = float.Parse(e_t_attr.Value);
						Matrix4x4 t_u = new Matrix4x4(
											new Vector4(1,		0,		0,		0)
										,	new Vector4(0,		1,		0,		0)
										,	new Vector4(0,		0,		1,		0)
										,	new Vector4(0,		e_t,	0,		1)
										);
						Matrix4x4 t_u_inv = new Matrix4x4(
											new Vector4(1,		0,		0,		0)
										,	new Vector4(0,		1,		0,		0)
										,	new Vector4(0,		0,		1,		0)
										,	new Vector4(0,		-e_t,	0,		1)
										);
						transform.Translate(new Vector3(0, e_t, 0), Space.World);
						XmlNodeList points = n_child.ChildNodes;
						Debug.Assert(null == points
									|| 3 == points.Count);
						if (null != points)
						{
							Vector3[] p_u = new Vector3[4];
							Vector3[] p_s = new Vector3[4];
							string [] name = {"x_u", "y_u", "z_u"
											, "x_s", "y_s", "z_s"};
							float [] val = new float[name.Length];
							for (int i_point = 0; i_point < 3; i_point ++)
							{
								XmlNode point_node = points[i_point];
								XmlElement point_ele = (XmlElement)point_node;
								for (int i_attr = 0; i_attr < name.Length; i_attr ++)
								{
									XmlElement v_text = point_ele[name[i_attr]];
									val[i_attr] = float.Parse(v_text.InnerXml);
								}
								p_u[i_point] = new Vector3(val[0], val[1], val[2]);
								p_s[i_point] = new Vector3(val[3], val[4], val[5]);
							}



							Vector4[] v4_u = new Vector4[4];
							Vector4[] v4_s = new Vector4[4];
							v4_u[0] = new Vector4(p_u[0].x, p_u[0].y, p_u[0].z, 1);
							v4_s[0] = new Vector4(p_s[0].x, p_s[0].y, p_s[0].z, 1);
							for (int i_v4 = 1; i_v4 < 3; i_v4 ++)
							{
								v4_u[i_v4] = new Vector4( p_u[i_v4].x - p_u[0].x
														, p_u[i_v4].y - p_u[0].y
														, p_u[i_v4].z - p_u[0].z
														, 0);
								v4_s[i_v4] = new Vector4( p_s[i_v4].x - p_s[0].x
														, p_s[i_v4].y - p_s[0].y
														, p_s[i_v4].z - p_s[0].z
														, 0);
							}
							Vector4 up_u = new Vector4(0, 1, 0, 0);
							Vector4 up_s = new Vector4(0, 0, 1, 0);
							v4_u[3] = up_u * (Vector4.Magnitude(v4_u[1]) + Vector4.Magnitude(v4_u[2])) * 0.5f;
							v4_s[3] = up_s * (Vector4.Magnitude(v4_s[1]) + Vector4.Magnitude(v4_s[2])) * 0.5f;

							Matrix4x4 m_u = new Matrix4x4(v4_u[0], v4_u[1], v4_u[2], v4_u[3]);
							Matrix4x4 m_s = new Matrix4x4(v4_s[0], v4_s[1], v4_s[2], v4_s[3]);

							if (DEF_LOGMATRIXFAC)
							{
								Matrix4x4 c_sim2unity_prime = t_u * m_u * m_s.inverse;
								Matrix4x4 c_unity2sim_prime = m_s * m_u.inverse * t_u_inv;
								float error_u = 0;
								float error_s = 0;
								for (int i = 0; i < 4; i++)
								{
									for (int j = 0; j < 4; j++)
									{
										float e = c_sim2unity_prime[i, j] - c_sim2unity[i, j];
										float e2 = Mathf.Abs(e);
										if (error_u < e2)
											error_u = e2;
										e = c_unity2sim_prime[i, j] - c_unity2sim[i, j];
										e2 = Mathf.Abs(e);
										if (error_s < e2)
											error_s = e2;
									}
								}
								string strLog = string.Format("Error sim2unity:{0}\nError unity2sim:{1}", error_u, error_s);
								Debug.LogWarning(strLog);
								c_sim2unity = c_sim2unity_prime;
								c_unity2sim = c_unity2sim_prime;
							}
							else
							{
								string strInfo = string.Format("matrix_s:\n{0}", m_s.ToString());
								strInfo += string.Format("matrix_u:\n{0}", m_u.ToString());
								Debug.Log(strInfo);
								c_sim2unity = t_u * m_u * m_s.inverse;
								c_unity2sim = m_s * m_u.inverse * t_u_inv;
							}
						}
						else
						{
							c_sim2unity = t_u * c_sim2unity;
							c_unity2sim = c_unity2sim * t_u_inv;
						}
					}
				}

				setLayer(gameObject, LAYER.scene_static);
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

	int m_iTeleport = 0;
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
									setLayer(o, LAYER.peer_dynamic);
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
									Vector3 p_sim = new Vector3((float)xPos, (float)yPos, (float)zPos);
									Vector3 t_sim = new Vector3((float)xTan, (float)yTan, (float)zTan);
									Vector3 l_sim = new Vector3((float)xLat, (float)yLat, (float)zLat);
									Vector3 p_unity = c_sim2unity.MultiplyPoint3x4(p_sim);
									Vector3 t_unity = MultiplyDir(c_sim2unity, t_sim);
									Vector3 l_unity = MultiplyDir(c_sim2unity, l_sim);
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
										m_confAvatar.setTeleport(p_sim, t_sim, l_sim);
										RootMotion.FinalIK.VRIK ik = ped.AddComponent<RootMotion.FinalIK.VRIK>();
										ped.AddComponent<RootMotion.FinalIK.VRIKBackup>();
										ik.AutoDetectReferences();

										bool mockTracking = (null != m_mockTrackersPrefab);
										if (mockTracking)
										{
											m_trackers = Instantiate(m_mockTrackersPrefab, p_unity, q_unity);
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
											caliCtrl.headTracker = m_trackers.transform.Find(targetNames[0]);
											caliCtrl.bodyTracker = m_trackers.transform.Find(targetNames[1]);
											caliCtrl.leftHandTracker = m_trackers.transform.Find(targetNames[2]);
											caliCtrl.rightHandTracker = m_trackers.transform.Find(targetNames[3]);
											caliCtrl.leftFootTracker = m_trackers.transform.Find(targetNames[4]);
											caliCtrl.rightFootTracker = m_trackers.transform.Find(targetNames[5]);
										}
										else
										{
											GameObject steamVR = GameObject.Find("[CameraRig]");
											Debug.Assert(null != steamVR);
											SteamVR_Manager mgr = steamVR.GetComponent<SteamVR_Manager>();
											mgr.m_avatar = ped;
											Debug.Assert(null != m_confAvatar);
											m_confAvatar.Apply(ik);
											m_trackers = steamVR;
										}
										setLayer(ped, LAYER.ego_dynamic);
										setLayer(m_trackers, LAYER.ego_dynamic);
										//no matter driver or pedestrain, by default, inspector is on avatar
										InspectorHelper inspector = new InspectorHelper(ped.transform, m_confAvatar);
										m_egoInspector = Instantiate(m_camInspectorPrefab).GetComponent<Camera>();
										inspector.Apply(m_egoInspector, InspectorHelper.Direction.forward);
									}
									else
										setLayer(ped, LAYER.peer_dynamic);


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
							case EVT.telPed:
								{
									int id;
									double xPos, yPos, zPos;
									double xTan, yTan, zTan;
									double xLat, yLat, zLat;
									m_ctrl.GettelPedTuple(out id
														, out xPos, out yPos, out zPos
														, out xTan, out yTan, out zTan
														, out xLat, out yLat, out zLat);
									Debug.Assert(0 == id); //currently, only one pedestrain supported
									Vector3 p = new Vector3((float)xPos, (float)yPos, (float)zPos);
									Vector3 t = new Vector3((float)xTan, (float)yTan, (float)zTan);
									Vector3 l = new Vector3((float)xLat, (float)yLat, (float)zLat);
									Teleport(p, t, l);
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
							Debug.Assert(null != m_trackers);
							m_trackers.transform.parent = parent.transform;
							bool steamTracking = (null == m_mockTrackersPrefab);
							if (steamTracking)
							{
								SteamVR_ManagerDrv mgr = m_trackers.GetComponent<SteamVR_ManagerDrv>();
								Debug.Assert(null != mgr);
								mgr.m_carHost = parent;
							}
							setLayer(parent, LAYER.host_dynamic);
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

				if (DEF_TESTTELEPORT
					&& Input.GetKeyDown(KeyCode.T)
					&& (Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.LeftAlt)))
				{
					Vector3 pos, tan, lat;
					m_confAvatar.testTeleport(++m_iTeleport, out pos, out tan, out lat);
					Teleport(pos, tan, lat);
				}

				if (DEF_LOGPEDTRANSIM)
				{
					CultureInfo ci = new CultureInfo("en-us");
					string strLog = "";
					foreach (KeyValuePair<int, GameObject> kv in m_id2Ped)
					{
						Transform tran_u = kv.Value.transform;
						Vector3 pos_s = c_unity2sim.MultiplyPoint3x4(tran_u.position);
						Vector3 tan_s = c_unity2sim.MultiplyVector(tran_u.forward);
						string strItem = string.Format("\n{0}:\n\tp=[{1} {2} {3}]\n\tt=[{4} {5} {6}]"
														, kv.Value.name
														, pos_s.x.ToString("E07", ci), pos_s.y.ToString("E07", ci), pos_s.z.ToString("E07", ci)
														, tan_s.x.ToString("E07", ci), tan_s.y.ToString("E07", ci), tan_s.z.ToString("E07", ci));
						strLog += strItem;
					}
					Debug.Log(strLog);
				}
			}
			catch (Exception e)
			{
				Debug.Log(e.Message);
			}
		}
	}

	static void FrameToQuaternionVehi(Vector3 t, Vector3 l, out Quaternion q)
	{
		Vector3 z_prime = Vector3.Cross(l, -t);
		Vector3 y_prime = -t;
		q = new Quaternion();
		q.SetLookRotation(z_prime, y_prime);
	}

	static void FrameToQuaternionPed(Vector3 t, Vector3 l, out Quaternion q)
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

	public float adjustAvatar(float dh)
	{
		return m_confAvatar.ScaleInv(dh);
	}

	void setLayer(GameObject o, LAYER l)
	{
		Queue<Transform> bfs = new Queue<Transform>();
		bfs.Enqueue(o.transform);
		while (bfs.Count > 0)
		{
			Transform t = bfs.Dequeue();
			t.gameObject.layer = (int)l;
			foreach (Transform t_c in t)
				bfs.Enqueue(t_c);
		}
	}

	public void adjustInspector(InspectorHelper.Direction dir, bool host)
	{
		InspectorHelper inspector = null;
		if (host)
			inspector = new InspectorHelper(m_id2Ped[0].transform.parent, m_confVehicle);
		else
			inspector = new InspectorHelper(m_id2Ped[0].transform, m_confAvatar);
		inspector.Apply(m_egoInspector, dir);
	}

}
