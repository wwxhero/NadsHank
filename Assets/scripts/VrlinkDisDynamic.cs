using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

struct GlobalId
{
    public uint owner; //owner's ip
    public ushort objId; //object's id
    public GlobalId(uint a_owner, ushort a_objId)
    {
        owner = a_owner;
        objId = a_objId;
    }
};
class VrlinkDisDynamic
{
    public bool Receive(GlobalId id_global, out Vector3 pos, out Vector3 tan, out Vector3 right)
    {
        //todo: recieve state information through vrlink
        pos = new Vector3();
        tan = new Vector3();
        right = new Vector3();
        return false;
    }

    public void NetworkInitialize(List<uint> senders, List<uint> receivers, int port, uint self)
    {
        //intialize vr-link connections
        Debug.Log("Out:");
        foreach (uint ipOut in senders)
        {
            byte [] ipSeg = BitConverter.GetBytes(ipOut);
            Debug.LogFormat("\t{0}.{1}.{2}.{3}", ipSeg[0], ipSeg[1], ipSeg[2], ipSeg[3]);
        }

        Debug.Log("In:");
        foreach (uint ipIn in receivers)
        {
            byte[] ipSeg = BitConverter.GetBytes(ipIn);
            Debug.LogFormat("\t{0}.{1}.{2}.{3}", ipSeg[0], ipSeg[1], ipSeg[2], ipSeg[3]);
        }

        Debug.LogFormat("port:{0}", port);

        byte[] seg = BitConverter.GetBytes(self);
        Debug.LogFormat("self:\t{0}.{1}.{2}.{3}", seg[0], seg[1], seg[2], seg[3]);
                               
    }

    public void NetworkUnInitialize()
    {
        //unintialize vr-link connections
    }

    public void PreDynaCalc()
    {
        //prepare environment for subsequent receiving and sending
    }

    public void PostDynaCalc()
    {
        //wrap up for network communication perframe
    }

    virtual public void CreateAdoStub(GlobalId id_global, string name, Vector3 pos, Vector3 forward, Vector3 right)
    {

    }

    virtual public void DeleteAdoStub(GlobalId id_global)
    {

    }
}

