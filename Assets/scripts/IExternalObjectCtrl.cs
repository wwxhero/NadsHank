using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;

interface IExternalObjectCtrl
{
    //todo: load distributed version scene file, intialize vr-link
    bool Initialize(CvedPed cved, XmlNode root);
    //todo: unload distributed version scene file, uninitialize vr-link
    void UnInitialize();
    //todo: prepare environment for network traffic
    void PreUpdateDynamicModels();
    //todo: wrap up the network traffic for a single frame
    void PostUpdateDynamicModels();
    //todo: recieve from neighbors for pos and orientation in left-hand convension
    bool OnGetUpdate(short id_local, ref Vector3 pos_state, ref Vector3 forward_state, ref Vector3 right_state);
    //todo: send out state information of a pedestrain as a partical
    void OnPushUpdate(short id_local, Vector3 pos_state, Vector3 tan_state, Vector3 lat_state);
}

