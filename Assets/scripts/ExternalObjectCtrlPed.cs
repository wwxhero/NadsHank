using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using UnityEngine;

class ExternalObjectCtrlPed : VrlinkPedestrainCtrl
                            , IExternalObjectCtrl
{
    enum Type { edo_controller = 0, ado_controller, ped_controller };
    Dictionary<ushort, GlobalId> m_mapLid2Gid = new Dictionary<ushort, GlobalId>();
    Dictionary<GlobalId, GameObject> m_mapGid2Ado;

    List<uint> m_ipClusters;

    uint m_selfIp;
    CvedPed m_pCved;
    Matrix4x4 c_sim2unity;

    struct SEG
    {
        uint ip;
        uint mask;
        public uint Group()
        {
            uint g = ip & mask;
            g = g | (~mask);
            return g;
        }
        public SEG(uint a_ip, uint a_mask)
        {
            ip = a_ip;
            mask = a_mask;
        }
    };
    private void InitIpclusters(List<SEG> ips, out List<uint> clusters)
    {
        //it is the hardcoded version of broadcast
        HashSet<uint> setIps = new HashSet<uint>();
        foreach (SEG seg in ips)
        {
            setIps.Add(seg.Group());
        }
        clusters = new List<uint>();
        foreach (uint ip in setIps)
        {
            clusters.Add(ip);
        }
    }
    private void BroadCastObj(ushort id_local, Vector3 pos_state, Vector3 forward_state, Vector3 right_state)
    {
        //todo: broadcast pedestrian state information
    }
    private void getLocalhostIps(out HashSet<uint> lstIps)
    {
        lstIps = new HashSet<uint>();
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                byte [] ipSeg = ip.GetAddressBytes();
                uint ipCode = BitConverter.ToUInt32(ipSeg, 0);
                lstIps.Add(ipCode);
            }
        }
    }

    public ExternalObjectCtrlPed()
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
    public bool Initialize(CvedPed cved, XmlNode root)
    {
        //todo: load distributed version scene file, intialize vr-link
        HashSet<uint> localhostIps;
        getLocalhostIps(out localhostIps);

        List<SEG> neighborsTo = new List<SEG>();
        List<uint> neighborsFrom = new List<uint>();
        ushort id_local = 0;
        int numSelf = 0;
        XmlNode distriConf = root.FirstChild;
        bool ok = true;
        while (null != distriConf)
        {
            XmlAttributeCollection attrs = distriConf.Attributes;
            string ipStr = attrs["ipv4"].Value;
            string ipMask = attrs["ipmask"].Value;
            uint simIp = BitConverter.ToUInt32(IPAddress.Parse(ipStr).GetAddressBytes(), 0);
            uint simMask = BitConverter.ToUInt32(IPAddress.Parse(ipMask).GetAddressBytes(), 0);
            string strType = attrs["type"].Value;
            int type = 0;
            ok = int.TryParse(strType, out type);
            Debug.Assert(ok);
            bool selfBlk = (localhostIps.Contains(simIp) && (int)Type.ped_controller == type); //0: edo_controller, 1: ado_controller, 2: ped_controller
            bool neighborBlk = !selfBlk;
            bool peerEdoBlk = ((int)Type.edo_controller == type
                            && neighborBlk);
            if (neighborBlk)
            {
                SEG seg = new SEG(simIp, simMask);
                neighborsTo.Add(seg);
                neighborsFrom.Add(simIp);
            }

            if (peerEdoBlk)
            {
                id_local = cved.CreateVehicle(attrs["cabtype"].Value);
                GlobalId id_global = new GlobalId(simIp, 0);
                m_mapLid2Gid.Add(id_local, id_global);
            }

            if (selfBlk)
            {
                m_selfIp = simIp;
                numSelf++;
                //fixme: intialize state0 of pedestrian and ready to send out
            }
            distriConf = distriConf.NextSibling;
        }

        ok = (numSelf == 1);
        if (ok)
        {
            InitIpclusters(neighborsTo, out m_ipClusters);
            string strPort = root.Attributes["port"].Value;
            int port;
            ok = int.TryParse(strPort, out port);
            Debug.Assert(ok); //port is in right format
            base.NetworkInitialize(m_ipClusters, neighborsFrom, port, m_selfIp);
            m_pCved = cved;
            //fixme: broadcast state0 to all neighbors
        }

        return ok;
    }

    public void UnInitialize()
    {
        //todo: unload distributed version scene file, uninitialize vr-link
        base.NetworkUnInitialize();
        m_ipClusters.Clear();
        m_mapLid2Gid.Clear();
    }
    public void PreUpdateDynamicModels()
    {
        //todo: prepare environment for network traffic
        base.PreDynaCalc();

    }
    public void PostUpdateDynamicModels()
    {
        //todo: wrap up the network traffic for a single frame
        base.PostDynaCalc();
    }

    public override void CreateAdoStub(GlobalId id_global, string name, Vector3 pos, Vector3 forward, Vector3 right)
    {
        //need pdu support from vr-link or work around
    }

    public override void DeleteAdoStub(GlobalId id_global)
    {
        //
    }

    public bool OnGetUpdate(ushort id_local, out Vector3 pos_state, out Vector3 forward_state, out Vector3 right_state)
    {
        //todo: recieve from neighbors for pos and orientation in left-hand convension
        forward_state = new Vector3(1, 0, 0);
        right_state = new Vector3(0, 0, -1);
        pos_state = new Vector3(-600f, 0.5f, -4290f);
        GlobalId id_global = new GlobalId(0, 0);
        if (!m_mapLid2Gid.TryGetValue(id_local, out id_global))
            return false;
        Vector3 pos_state_sim;
        Vector3 forward_state_sim;
        Vector3 right_state_sim;
        bool recieved = base.Receive(id_global, out pos_state_sim, out forward_state_sim, out right_state_sim);
        if (recieved)
        {
            pos_state = c_sim2unity.MultiplyPoint3x4(pos_state_sim);
            forward_state = c_sim2unity.MultiplyVector(forward_state_sim);
            right_state = c_sim2unity.MultiplyVector(right_state_sim);
        }

        string[] recFlag = { "NReceived", "Recieved" };
        Byte[] seg = BitConverter.GetBytes(id_global.owner);
        int idx = recieved ? 1 : 0;
        Debug.LogFormat(@"OnGetUpdate {0} id:{1} from ip:[{2}.{3}.{4}.{5}]
                                position: [{6},{7},{8}]
                                tangent: [{9},{10},{11}]
                                lateral: [{12},{13},{14}]"
                                , recFlag[idx], id_local, seg[0], seg[1], seg[2], seg[3]
                                , pos_state.x, pos_state.y, pos_state.z
                                , forward_state.x, forward_state.y, forward_state.z
                                , right_state.x, right_state.y, right_state.z);
        return recieved;
    }
    public void OnPushUpdate(ushort id_local, Vector3 pos_state, Vector3 tan_state, Vector3 lat_state)
    {
        //todo: send out state information of a pedestrain as a partical
    }

}