using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SharpCom;
using ExternalObjectsControlComLib;
using System.Xml;

class Joint
{
    public Joint(Transform t, Transform t_base)
    {
        //fixme: unnecessry computation for supported joints from DIGUY
        m_t = t;
        m_q0 = t.localRotation;

        Vector3 x_d_ub = new Vector3(0, 0, 1);
        Vector3 y_d_ub = new Vector3(-1, 0, 0);
        Vector3 z_d_ub = new Vector3(0, 1, 0);
        Matrix4x4 m_w2l = t.worldToLocalMatrix;
        Matrix4x4 m_b2w = t_base.localToWorldMatrix;
        Matrix4x4 m_b2l = m_w2l * m_b2w;
        Vector3 x_d_ul = m_b2l.MultiplyVector(x_d_ub);
        Vector3 y_d_ul = m_b2l.MultiplyVector(y_d_ub);
        Vector3 z_d_ul = m_b2l.MultiplyVector(z_d_ub);
        Vector4 x_d_ul_m = new Vector4(x_d_ul.x, x_d_ul.y, x_d_ul.z, 0);
        Vector4 y_d_ul_m = new Vector4(y_d_ul.x, y_d_ul.y, y_d_ul.z, 0);
        Vector4 z_d_ul_m = new Vector4(z_d_ul.x, z_d_ul.y, z_d_ul.z, 0);
        Vector4 w_d_ul_m = new Vector4(0, 0, 0, 1);
        m_d2u.SetColumn(0, x_d_ul_m);
        m_d2u.SetColumn(1, y_d_ul_m);
        m_d2u.SetColumn(2, z_d_ul_m);
        m_d2u.SetColumn(3, w_d_ul_m);
        m_u2d = m_d2u.inverse;
    }
    public readonly Transform m_t;
    public readonly Quaternion m_q0;
    public readonly Matrix4x4 m_u2d;
    public readonly Matrix4x4 m_d2u;
};


public class ScenarioControlPed : MonoBehaviour {

    private string m_scenePath;
    public GameObject[] m_vehiPrefabs;
    public GameObject m_pedPrefab;
    IDistriObjsCtrl m_ctrl;
    Dictionary<int, GameObject> m_id2Dyno = new Dictionary<int, GameObject>();
    Dictionary<int, GameObject> m_id2Ped = new Dictionary<int, GameObject>();
    //map: id->n_part
    Dictionary<int, int>        m_id2PedPartN = new Dictionary<int, int>();

    //map: (id, i_part)->Joint.transform.localRotation
    Dictionary<long, Joint> m_partId2tran = new Dictionary<long, Joint>();
    Matrix4x4 c_sim2unity;
    Matrix4x4 c_unity2sim;
    enum IMPLE { IGCOMM = 0, DISVRLINK };
    enum TERMINAL { edo_controller = 0, ado_controller, ped_controller };
    long PartID_U(int idPed, int idPart_S)
    {
        long partId_g = idPed;
        partId_g = (partId_g << 32);
#pragma warning disable 0675
        partId_g = (partId_g | idPart_S);
        return partId_g;
    }

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
                                    int idx = id % m_vehiPrefabs.Length;
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
                                    GameObject ped = Instantiate(m_pedPrefab, p_unity, q_unity);
                                    ped.name = name;
                                    m_id2Ped.Add(id, ped);
                                    Transform t_base = ped.transform.Find("CMU compliant skeleton");
                                    InitZeroPos.Init(t_base);
                                    if (0 == id)
                                    {
                                        ped.AddComponent<Manipulator>();
                                        //ped.AddComponent<Roll>();
                                        //ped.AddComponent<JointDumper>();
                                        RootMotion.FinalIK.VRIK ik = ped.AddComponent<RootMotion.FinalIK.VRIK>();
                                        ik.AutoDetectReferences();
                                        ik.enabled = false;
                                    }


                                    m_id2PedPartN.Add(id, nParts);

                                    for (int i_part = 0; i_part < nParts; i_part ++)
                                    {
                                        string namePartS;
                                        m_ctrl.GetcrtPedPartName(id, i_part, out namePartS);
                                        string log = string.Format("{0}:{1}", i_part, namePartS);
                                        Debug.Log(log);
                                        GameObject go = GameObject.Find(namePartS);
                                        Debug.Assert(null != go);
                                        Transform tran = go.transform;
                                        Debug.Assert(null != tran);
                                        long partId = PartID_U(id, i_part);
                                        Joint j = new Joint(tran, t_base);
                                        m_partId2tran.Add(partId, j);
                                    }

                                    break;
                                }
                            case EVT.delPed:
                                {
                                    int id;
                                    m_ctrl.GetdelPedTuple(out id);

                                    int n_parts = 0;
                                    if (m_id2PedPartN.TryGetValue(id, out n_parts))
                                    {
                                        for (int i_part = 0; i_part < n_parts; i_part ++)
                                        {
                                            long partId = PartID_U(id, i_part);
                                            m_partId2tran.Remove(partId);
                                        }
                                        m_id2PedPartN.Remove(id);
                                    }


                                    GameObject o;
                                    if (m_id2Ped.TryGetValue(id, out o))
                                    {
                                        m_id2Ped.Remove(id);
                                        GameObject.Destroy(o);
                                    }

                                    break;
                                }
                        }
                        m_ctrl.QPopEvent();
                    }
                } while (!nonEvt);

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

                    int nParts = m_id2PedPartN[c_ownPedId];
                    double q_w, q_x, q_y, q_z;
                    for (int i_part = 0; i_part < nParts; i_part ++)
                    {
                        long partId = PartID_U(c_ownPedId, i_part);
                        Joint joint = m_partId2tran[partId];
                        Quaternion q_unity = joint.m_t.localRotation;
                        Quaternion q_0_inv = Quaternion.Inverse(joint.m_q0);
                        Quaternion q_unity_offset =  q_0_inv * q_unity;
                        JointQuatU2D(q_unity_offset, out q_w, out q_x, out q_y, out q_z, joint.m_u2d);
                        Quaternion q_sim_offset = new Quaternion((float)q_x, (float)q_y, (float)q_z, (float)q_w);

//fixme debugging log:
                        Vector3 a_unity_offset = q_unity_offset.eulerAngles;
                        float epsilon_f = 0.1f;
                        if (a_unity_offset.x < -epsilon_f || a_unity_offset.x > epsilon_f
                            || a_unity_offset.y < -epsilon_f || a_unity_offset.y > epsilon_f
                            || a_unity_offset.z < -epsilon_f || a_unity_offset.z > epsilon_f)
                        {
                            Vector3 a_sim_offset = q_sim_offset.eulerAngles;
                            Vector3 a_unity_base = joint.m_q0.eulerAngles;
                            Vector3 a_unity_prime = joint.m_t.localEulerAngles;
                            string strLog = string.Format("\t{0}:\t[{1} {2} {3}]_u===>[{4} {5} {6}]_s\n", joint.m_t.name
                                                                                                        , a_unity_offset.x, a_unity_offset.y, a_unity_offset.z
                                                                                                        , a_sim_offset.x, a_sim_offset.y, a_sim_offset.z);
                            strLog += string.Format("\t\t[{0} {1} {2}]*[{3} {4} {5}]==[{6} {7} {8}]\n", a_unity_base.x, a_unity_base.y, a_unity_base.z
                                                                                                      , a_unity_offset.x, a_unity_offset.y, a_unity_offset.z
                                                                                                      , a_unity_prime.x, a_unity_prime.y, a_unity_prime.z);
                            Debug.Log(strLog);
                        }
//end of debugging log

                        m_ctrl.OnPushUpdateArt(c_ownPedId, i_part, q_w, q_x, q_y, q_z);
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
                        double q_s_w, q_s_x, q_s_y, q_s_z;
                        int nParts = m_id2PedPartN[kv.Key];
                        for (int i_part = 0; i_part < nParts; i_part ++)
                        {
                            //fixme: performance might be sacrified here from loop manage to native code call
                            long partId = PartID_U(kv.Key, i_part);
                            Joint joint = m_partId2tran[partId];
                            m_ctrl.OnGetUpdateArt(kv.Key, i_part, out q_s_w, out q_s_x, out q_s_y, out q_s_z);
                            Quaternion q_unity_offset;
                            JointQuatD2U(q_s_w, q_s_x, q_s_y, q_s_z, out q_unity_offset, joint.m_d2u);
                            q_unity = joint.m_q0 * q_unity_offset;
                            Transform tran = joint.m_t;
                            tran.localRotation = q_unity;
                        }
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

}
