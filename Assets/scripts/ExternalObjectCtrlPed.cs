using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

class ExternalObjectCtrlPed : VrlinkPedestrainCtrl
                            , IExternalObjectCtrl
{
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
    public bool OnGetUpdate(short id_local, ref Vector3 pos_state, ref Vector3 forward_state, ref Vector3 right_state)
    {
        //todo: recieve from neighbors for pos and orientation in left-hand convension
        forward_state = new Vector3(1, 0, 0);
        right_state = new Vector3(0, 0, -1);
        pos_state = new Vector3(-600f, 0.5f, -4290f);

        return true;
    }
    public void OnPushUpdate(short id_local, Vector3 pos_state, Vector3 tan_state, Vector3 lat_state)
    {
        //todo: send out state information of a pedestrain as a partical
    }

}