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
    Dictionary<ushort, GlobalId> m_mapLid2Gid;
    Dictionary<GlobalId, GameObject> m_mapGid2Ado;

    List<ulong> m_ipClusters;
    List<GameObject> m_lstPeers;
    ulong m_selfIp;
    CvedPed m_pCved;

    struct SEG
    {
        ulong ip;
        ulong mask;
        ulong Group()
        {
            ulong g = ip & mask;
            g = g | (~mask);
            return g;
        }
    };
    private void InitIpclusters(List<SEG> ips, ref List<ulong> clusters)
    {
    }
    //private void BroadCastObj(ushort id_local, const cvTObjStateBuf* sb);
    private void getLocalhostIps(ref List<ulong> lstIps)
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                ulong ipCode = BitConverter.ToUInt64(ip.GetAddressBytes(), 0);
                lstIps.Add(ipCode);
            }
        }
    }

    public ExternalObjectCtrlPed()
    {
    }
    public bool Initialize(CvedPed cved, XmlNode root)
    {
        //todo: load distributed version scene file, intialize vr-link
        return false;
    }

    public void UnInitialize()
    {
        //todo: unload distributed version scene file, uninitialize vr-link
    }
    public void PreUpdateDynamicModels()
    {
        //todo: prepare environment for network traffic
    }
    public void PostUpdateDynamicModels()
    {
        //todo: wrap up the network traffic for a single frame
    }
    public bool OnGetUpdate(ushort id_local, ref Vector3 pos_state, ref Vector3 forward_state, ref Vector3 right_state)
    {
        //todo: recieve from neighbors for pos and orientation in left-hand convension
        forward_state = new Vector3(1, 0, 0);
        right_state = new Vector3(0, 0, -1);
        pos_state = new Vector3(-600f, 0.5f, -4290f);

        return true;
    }
    public void OnPushUpdate(ushort id_local, Vector3 pos_state, Vector3 tan_state, Vector3 lat_state)
    {
        //todo: send out state information of a pedestrain as a partical
    }

}