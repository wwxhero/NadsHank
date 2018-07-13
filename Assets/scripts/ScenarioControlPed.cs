using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SharpCom;
using ExternalObjectsControlComLib;

public class ScenarioControlPed : MonoBehaviour {

    public string m_scenePath;
    public GameObject[] m_prefabs;
    IDistriObjsCtrl m_ctrl;
    Dictionary<int, GameObject> m_id2Dyno = new Dictionary<int,GameObject>();
    Matrix4x4 c_sim2unity;
    enum IMPLE { IGCOMM = 0, DISVRLINK };
    enum TERMINAL { edo_controller = 0, ado_controller, ped_controller };
    float c_scale = 2.5f;
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
        c_sim2unity = m_2 * m_1;
    }



	void Start () {
        if (null == m_ctrl)
        {
            try
            {
                m_ctrl = new DistriObjsCtrlClass();
                m_ctrl.CreateNetworkExternalObjectControl((int)IMPLE.DISVRLINK, (int)TERMINAL.ped_controller);
                m_ctrl.Initialize(m_scenePath);
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
                                    Vector3 t_unity = c_sim2unity.MultiplyVector(t);
                                    Vector3 l_unity = c_sim2unity.MultiplyVector(l);
                                    Quaternion q_unity;
                                    FrameToQuaternion(t_unity, l_unity, out q_unity);
                                    int idx = id % m_prefabs.Length;
                                    GameObject o = Instantiate(m_prefabs[idx], p_unity, q_unity);
                                    o.name = name;
                                    o.transform.localScale = new Vector3(c_scale, c_scale, c_scale);
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
                        }
                        m_ctrl.QPopEvent();
                    }
                } while (!nonEvt);

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
                        Vector3 t_unity = c_sim2unity.MultiplyVector(t);
                        Vector3 l_unity = c_sim2unity.MultiplyVector(l);
                        Quaternion q_unity;
                        FrameToQuaternion(t_unity, l_unity, out q_unity);
                        kv.Value.transform.position = p_unity;
                        kv.Value.transform.rotation = q_unity;
                    }
                    string strTuple = string.Format("\nid = {10} received = {0}:\n\tpos=[{1},{2},{3}]\n\ttan=[{4},{5},{6}]\n\tlat=[{7},{8},{9}]"
                                                        , received, xPos, yPos, zPos, xTan, yTan, zTan, xLat, yLat, zLat, kv.Key);
                    Debug.Log(strTuple);
                }

                m_ctrl.PostUpdateDynamicModels();


            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }
	}

    void FrameToQuaternion(Vector3 t, Vector3 l, out Quaternion q)
    {
        Vector3 z_prime = Vector3.Cross(l, -t);
        Vector3 y_prime = -t;
        q = new Quaternion();
        q.SetLookRotation(z_prime, y_prime);
    }
}
