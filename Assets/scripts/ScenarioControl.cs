using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using UnityEngine;

public class ScenarioControl : MonoBehaviour {
    public GameObject [] m_prefabs;
    public string[] m_typeNames;
    CvedPed m_cved;
	IExternalObjectCtrl m_extCtrl;
    const bool c_debug = true;
    List<ushort> m_lstVehis;
    // Use this for initialization
    void Start ()
    {

    }

    void OnEnable()
    {
        try
        {
            m_cved = new CvedPed();
            m_extCtrl = new ExternalObjectCtrlPed();
            XmlDocument scene = new XmlDocument();
            scene.Load("SceneDistri.xml");
            XmlNode root = scene.DocumentElement;
            m_cved.Initialize(m_extCtrl, m_prefabs, m_typeNames);
            m_extCtrl.Initialize(m_cved, root);


            if (c_debug)
                m_lstVehis = new List<ushort>();
        }
        catch(System.IO.FileNotFoundException)
        {
            Debug.LogError("can't find scene file");
            OnDisable();
        }
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
        ushort id_local = m_cved.CreateVehicle("test" + numVehis
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
            ushort id_local = m_lstVehis[idx_t];
            m_cved.DeleteVehicle(id_local);
            m_lstVehis.RemoveAt(idx_t);
        }
    }
    void testGetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                Debug.Log("ip:" + ip.ToString());
            }
        }
        //throw new Exception("No network adapters with an IPv4 address in the system!");
    }
    void testLoadScene()
    {
        try
        {
            XmlDocument scene = new XmlDocument();
            scene.Load("SceneDistri.xml");
            XmlNode root = scene.DocumentElement;
            XmlAttribute attr_root = root.Attributes["port"];
            Debug.Log("root:" + "name=" + root.Name+ " port="+attr_root.Value);
            XmlNode c = root.FirstChild;
            while(null != c)
            {
                XmlAttributeCollection attr_child = c.Attributes;
                int type = Convert.ToInt32(attr_child["type"].Value);
                if (1 == type)
                    Debug.Log("child:"
                            + "name=" + c.Name
                            + " name2=" + attr_child["name"].Value
                            + " type=" + attr_child["type"].Value
                            + " ipv4=" + attr_child["ipv4"].Value
                            + " ipmask=" + attr_child["ipmask"].Value);
                else if(0 == type)
                    Debug.Log("child:"
                            + "name=" + c.Name
                            + " name2=" + attr_child["name"].Value
                            + " type=" + attr_child["type"].Value
                            + " ipv4=" + attr_child["ipv4"].Value
                            + " ipmask=" + attr_child["ipmask"].Value
                            + " cabtype=" + attr_child["cabtype"].Value);
                c = c.NextSibling;
            }

        }
        catch (System.IO.FileNotFoundException)
        {
            Debug.Log("scene load failed!");
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
            if (GUI.Button(new Rect(ptBtn[0], ptBtn[1], szBtn[0], szBtn[1]), "testLoadScene"))
                testLoadScene();
            ptBtn.y = ptBtn.y + szBtn.y;
            if (GUI.Button(new Rect(ptBtn[0], ptBtn[1], szBtn[0], szBtn[1]), "testGetLocalIPAddress"))
                testGetLocalIPAddress();
            ptBtn.y = ptBtn.y + szBtn.y;
        }

	}
}
