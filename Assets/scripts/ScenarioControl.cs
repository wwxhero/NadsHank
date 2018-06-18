using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioControl : MonoBehaviour {
    public GameObject [] m_prefabs;
    CvedPed m_cved;
	ExternalObjectCtrl m_extCtrl;
    const bool c_debug = true;
    List<short> m_lstVehis;
    // Use this for initialization
	void Start ()
    {

    }

    void OnEnable()
    {
        m_cved = new CvedPed();
        m_extCtrl = new ExternalObjectCtrl();
        m_extCtrl.Initialize(m_cved);
        m_cved.Initialize(m_extCtrl, m_prefabs);

        if (c_debug)
            m_lstVehis = new List<short>();
    }

    void OnDisable()
    {
        m_extCtrl.UnInitialize();
    }



    void testCvedCreateVehicle()
    {
        Vector3 forward_state = new Vector3(1, 0, 0);
        Vector3 right_state = new Vector3(0, 0, -1);
        Vector3 pos_state = new Vector3(-600f, 0.5f, -4290f);
        uint numVehis = (uint)m_lstVehis.Count;
        short id_local = m_cved.CreateVehicle("test" + numVehis
                            , numVehis
                            , 8, 8, 8
                            , pos_state
                            , forward_state
                            , right_state);

        if (!(id_local < 0))
        {
        	m_lstVehis.Add(id_local);
        }

	}

    void testCvedDeleteVehicle()
    {
        int cnt = m_lstVehis.Count;
        if (cnt > 0)
        {
            int idx_t = cnt - 1;
            short id_local = m_lstVehis[idx_t];
            m_cved.DeleteVehicle(id_local);
            m_lstVehis.RemoveAt(idx_t);
        }
    }

	// Update is called once per frame
	void Update ()
    {
		m_cved.ExecuteDynamicModels();
	}
	void OnGUI()
    {
        if (c_debug)
        {
            Vector2 ptBtn = new Vector2(10, 10);
            Vector2 szBtn = new Vector2(200, 50);
            if (GUI.Button(new Rect(ptBtn[0], ptBtn[1], szBtn[0], szBtn[1]), "testCvedCreateVehicle"))
                testCvedCreateVehicle();
            ptBtn.y = ptBtn.y + szBtn.y;
            if (GUI.Button(new Rect(ptBtn[0], ptBtn[1], szBtn[0], szBtn[1]), "testCvedDeleteVehicle"))
                testCvedDeleteVehicle();
            ptBtn.y = ptBtn.y + szBtn.y;
        }

	}
}
