using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using makVrl;
struct GlobalId
{
    public uint owner; //owner's ip
    public ushort objId; //object's id
    public GlobalId(uint a_owner, ushort a_objId)
    {
        owner = a_owner;
        objId = a_objId;
    }
    public GlobalId(ushort a_siteId, ushort a_hostId, ushort a_objId)
    {
        //little endian layout: xxx.xxx.xxx.xxx
        //                     |siteId |hostId |
        owner = (uint)(a_siteId | (a_hostId << 16));
        objId = a_objId;
    }
};
class VrlinkDisDynamic
{
    struct VrlinkConf
    {
        public int drAlgor;
        public int stmType;
        public bool smoothOn;

        public double translationThreshold;
        public double rotationThreshold;

        public double latitude;
        public double longitude;
        public double viewOriPsi;
        public double viewOriTheta;
        public double viewOriPhi;

        //entity type
        public int kind;
        public int domain;
        public int country;
        public int category;

        public int subCategory;
        public int specific;
        public int extra;
    };

    struct EntityPub
    {
        public EntityPublisher pub;
        public EntityStateRepository esr;
    };

    struct EntityState
    {
        public Vector3 pos;
        public Vector3 forward;
        public Vector3 right;
        public bool updated;
    };

    struct CnnOut
    {
        public ExerciseConnection cnn;
        public Dictionary<ushort, EntityPub> pubs;
    };

    struct EntityPubRT
    {
        public ExerciseConnection cnn;
        public EntityPublisher pub;
        public EntityStateRepository esr;
        public EntityPubRT(ExerciseConnection a_cnn, EntityPublisher a_pub, EntityStateRepository a_esr)
        {
            cnn = a_cnn;
            pub = a_pub;
            esr = a_esr;
        }
    };

    Dictionary<uint, CnnOut> m_cnnsOut;
    Dictionary<GlobalId, EntityState> m_statesIn;

    ExerciseConnection m_cnnIn;
    ReflectedEntityList m_entitiesIn;

    static VrlinkConf s_disConf;
    ClockStaticAln m_sysClk;
    uint m_self;

    bool getEntityPub(uint ip, GlobalId id_global, out EntityPubRT pubRT)
    {
        ushort objId = id_global.objId;
        CnnOut cnnout;
        bool cnnHit = false;
        bool pubHit = false;
        EntityPub epb;
        if ((cnnHit = m_cnnsOut.TryGetValue(ip, out cnnout))
            && (pubHit = cnnout.pubs.TryGetValue(objId, out epb)))
        {
            ExerciseConnection cnn = cnnout.cnn;
            EntityStateRepository esr = epb.esr;
            EntityPublisher pub = epb.pub;
            pubRT = new EntityPubRT(cnn, pub, esr);
            return true;
        }
        else
        {
            pubRT = new EntityPubRT(null, null, null);
            return false;
        }
    }

    GlobalId VrlinkId2GlobalId(string entityId)
    {
        char [] seperator = {':'};
        string[] segs = entityId.Split(seperator);
        ushort siteId = 0;
        ushort hostId = 0;
        ushort entityNum = 0;
        if (UInt16.TryParse(segs[0], out siteId)
            && UInt16.TryParse(segs[1], out hostId)
            && UInt16.TryParse(segs[2], out entityNum))
        {
            GlobalId id_global = new GlobalId(siteId, hostId, entityNum);
            return id_global;
        }
        else
        {
            Debug.Assert(false); //entityId is not in right format
            return new GlobalId(0, 0, 0);
        }
    }

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

