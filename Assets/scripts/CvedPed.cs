using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class CvedPed : Object
{

    GameObject [] m_prefabs;
    Vector3 [] m_szPrefabs;
    IExternalObjectCtrl m_ctrl;
    Dictionary<ushort, GameObject> m_dictVehis;
    ushort m_keyBase = 1; //0 is reserved for self owned object
    Dictionary<string, GameObject> m_typename2prefab;

	public CvedPed()
	{

	}

	public bool Initialize(IExternalObjectCtrl ctrl, GameObject[] prefabs, string[] typenames)
	{
		m_ctrl = ctrl;
        m_dictVehis = new Dictionary<ushort, GameObject>();
        m_typename2prefab = new Dictionary<string, GameObject>();
        m_prefabs = prefabs;
        m_szPrefabs = new Vector3[prefabs.Length];
        for (int i = 0; i < prefabs.Length; i ++)
        {
            m_szPrefabs[i] = prefabs[i].GetComponent<Renderer>().bounds.size;
            m_typename2prefab[typenames[i]] = prefabs[i];
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
    public ushort CreateVehicle(string name
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
        ushort id_local = m_keyBase;
        m_dictVehis[id_local] = veh;
        m_keyBase++;
        return id_local;
    }

    public ushort CreateVehicle(string type)
    {
        //todo: create a vehicle and place it at default(invisible) place
        GameObject vehiPrefab = null;
        bool exists = m_typename2prefab.TryGetValue(type, out vehiPrefab);
        if (!exists)
        {
            System.Random rnd = new System.Random();
            vehiPrefab = m_prefabs[rnd.Next(m_prefabs.Length)];
        }
        GameObject veh = Instantiate(vehiPrefab);
        ushort id_local = m_keyBase;
        m_dictVehis[id_local] = veh;
        m_keyBase++;
        return id_local;
    }

    public GameObject GetVehicle(ushort id_local)
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

    public void DeleteVehicle(ushort id_local)
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
        foreach (ushort id_local in m_dictVehis.Keys)
        {
            Vector3 pos_state;
            Vector3 tan_state;
            Vector3 lat_state;
            m_ctrl.OnGetUpdate(id_local, out pos_state, out tan_state, out lat_state);
            GameObject o = m_dictVehis[id_local];
            o.transform.position = pos_state;
            Vector3 z_prime = Vector3.Cross(lat_state, -tan_state);
            Vector3 y_prime = -tan_state;
            Quaternion q = new Quaternion();
            q.SetLookRotation(z_prime, y_prime);
        }

    }

};