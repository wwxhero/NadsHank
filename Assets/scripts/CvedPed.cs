using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class CvedPed : Object
{

    GameObject [] m_prefabs;
    Vector3 [] m_szPrefabs;
    IExternalObjectCtrl m_ctrl;
    Dictionary<short, GameObject> m_dictVehis;
    short m_keyBase = 1; //0 is reserved for self owned object


	public CvedPed()
	{

	}

	public bool Initialize(IExternalObjectCtrl ctrl, GameObject[] prefabs)
	{
		m_ctrl = ctrl;
        m_dictVehis = new Dictionary<short, GameObject>();
        m_prefabs = prefabs;
        m_szPrefabs = new Vector3[prefabs.Length];
        for (int i = 0; i < prefabs.Length; i ++)
        {
            m_szPrefabs[i] = prefabs[i].GetComponent<Renderer>().bounds.size;
        }
        return true;
	}

    float mid(float k1, float k2, float k3)
    {
        float d_2_1 = k2 - k1;
        float d_3_2 = k3 - k2;
        if ((d_2_1 > 0) == (d_3_2 > 0))
            return k2;
        else if (Mathf.Abs(d_2_1) < Mathf.Abs(d_3_2))
            return k1;
        else
            return k3;
    }
    public short CreateVehicle(string name
                            , uint solId
                            , double xSize
                            , double ySize
                            , double zSize
                            , Vector3 ptPos
                            , Vector3 t
                            , Vector3 r)
    {
        //todo: create a vehicle object and return its id
        Debug.Assert(m_prefabs.Length > 0);
        uint idx_prefab = solId % (uint)m_prefabs.Length;
        Vector3 z_prime = Vector3.Cross(r, -t);
        Vector3 y_prime = -t;
        Quaternion q = new Quaternion();
        q.SetLookRotation(z_prime, y_prime);
        GameObject veh = Instantiate(m_prefabs[idx_prefab], ptPos, q);

        Vector3 szVeh = m_szPrefabs[idx_prefab];
        float k1 = (float)xSize / szVeh[0];
        float k2 = (float)ySize / szVeh[1];
        float k3 = (float)zSize / szVeh[2];
        float k = mid(k1, k2, k3);
        veh.transform.localScale = new Vector3(k, k, k);
        short id_local = m_keyBase;
        m_dictVehis[id_local] = veh;
        m_keyBase++;
        return id_local;
    }

    public GameObject GetVehicle(short id_local)
    {
        //todo: return the vehicle game object
        try
        {
            return m_dictVehis[id_local];
        }
        catch (KeyNotFoundException )
        {
            return null;
        }
    }

    public void DeleteVehicle(short id_local)
    {
        //remove the vehicle
        try
        {
            GameObject o = m_dictVehis[id_local];
            Destroy(o);
            m_dictVehis.Remove(id_local);
        }
        catch (KeyNotFoundException )
        {

        }
    }

	public void ExecuteDynamicModels()
	{
        //todo: update every vehicle's state
        //Debug.Log("ExecuteDynamicModels");
        Vector3 pos_state = new Vector3();
        Vector3 tan_state = new Vector3();
        Vector3 lat_state = new Vector3();
        foreach (short id_local in m_dictVehis.Keys)
        {
            m_ctrl.OnGetUpdate(id_local, ref pos_state, ref tan_state, ref lat_state);
            GameObject o = m_dictVehis[id_local];
            o.transform.position = pos_state;
            Vector3 z_prime = Vector3.Cross(lat_state, -tan_state);
            Vector3 y_prime = -tan_state;
            Quaternion q = new Quaternion();
            q.SetLookRotation(z_prime, y_prime);
        }

    }

};