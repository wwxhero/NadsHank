using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

struct GlobalId
{
    public ulong owner; //owner's ip
    public ushort objId; //object's id
    public GlobalId(ulong a_owner, ushort a_objId)
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

    public void NetworkInitialize(List<ulong> senders, List<ulong> receivers, int port, ulong self)
    {
        //intialize vr-link connections
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

