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
        public DeadReckonTypes drAlgor;
        public bool useAbsStamp;
        public bool smoothOn;

        public float translationThreshold;
        public float rotationThreshold;

        public double latitude;
        public double longitude;
        public double viewOriPsi;
        public double viewOriTheta;
        public double viewOriPhi;

        //entity type
        public EntityKind kind;
        public PlatformDomain domain;
        public CountryCode country;
        public int category;

        public int subCategory;
        public int specific;
        public int extra;

        public VrlinkConf(
            DeadReckonTypes a_drAlgor
            , bool a_useAbsStamp
            , bool a_smoothOn
            , float a_tThreshold, float a_rThreshold
            , double a_lati, double a_longi
            , double a_psi, double a_theta, double a_phi
            , EntityKind a_kind
            , PlatformDomain a_domain
            , CountryCode a_ccode
            , int a_cat
            , int a_subcat
            , int a_speci, int a_extra)
        {
            drAlgor = a_drAlgor; useAbsStamp = a_useAbsStamp; smoothOn = a_smoothOn;
            translationThreshold = a_tThreshold; rotationThreshold = a_rThreshold;
            latitude = a_lati; longitude = a_longi;
            viewOriPhi = a_psi; viewOriTheta = a_theta; viewOriPsi = a_psi;
            kind = a_kind; domain = a_domain; country = a_ccode;
            category = a_cat; subCategory = a_subcat; specific = a_speci; extra = a_extra;
        }



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
        public CnnOut(ExerciseConnection a_cnn)
        {
            cnn = a_cnn;
            pubs = new Dictionary<ushort, EntityPub>();
        }
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
    static VrlinkConf s_disConf = new VrlinkConf(
                                      DeadReckonTypes.Rvw
	                                , true //0:DtTimeStampRelative 1:DtTimeStampAbsolute
	                                , false
	                                , 0.05f						//translation threshold in meter
	                                , 0.05236f					//rotation threshold in radian = 3 degree
	                                , MathHelper.DegreesToRadians(35.699760)		//latitude
	                                , MathHelper.DegreesToRadians(-121.326577)	    //longitude
	                                , 0
	                                , 0
	                                , 0
	                                , EntityKind.Platform
	                                , PlatformDomain.PlatformDomainLand
	                                , CountryCode.UnitedStates
                                    , (int)PlatformAir.Fighter
	                                , (int)PlatformAirUSFighter.F18
	                                , 0, 0
                                  );
    ClockStaticAln m_sysClk;
    uint m_self;

    CoordinateTransform m_trans;

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


    public void Eulor2Frame(TaitBryan ori, out Vector3 tan, out Vector3 right, out Vector3 up)
    {
        Vector3 c_up0 = new Vector3(0, 0, 1);
        Vector3 c_t0 = new Vector3(0, -1, 0);
        Vector3 c_r0 = new Vector3(-1, 0, 0);

        float psi = (float)ori.psi;
        float theta = (float)ori.theta;
        float phi = (float)ori.phi;
        float c1 = Mathf.Cos(psi);
        float s1 = Mathf.Sin(psi);
        float c2 = Mathf.Cos(theta);
        float s2 = Mathf.Sin(theta);
        float c3 = Mathf.Cos(phi);
        float s3 = Mathf.Sin(phi);
        //float [,] m = new float[3, 3] {{c1*c2					,c2*s1					,-s2}
        //								,{c1*s2*s3 - c3*s1		,c1*c3 + s1*s2*s3		,c2*s3}
        //								,{s1*s3 + c1*c3*s2		,c3*s1*s2 - c1*s3		,c2*c3}};
        float[,] m = new float[3, 3] {{c1*c2		,c1*s2*s3 - c3*s1		,s1*s3 + c1*c3*s2}
									  ,{c2*s1		,c1*c3 + s1*s2*s3		,c3*s1*s2 - c1*s3}
									  ,{-s2			,c2*s3					,c2*c3}};
        Matrix4x4 mUnity = Matrix4x4.identity;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
                mUnity[i, j] = m[i, j];
        }

        tan = mUnity.MultiplyVector(c_t0);
        right = mUnity.MultiplyVector(c_r0);
        up = mUnity.MultiplyVector(c_up0);
    }

    public bool Receive(GlobalId id_global, out Vector3 pos, out Vector3 tan, out Vector3 right)
    {
        //todo: recieve state information through vrlink
        EntityState es;
        if (m_statesIn.TryGetValue(id_global, out es)
            && es.updated)
        {
            pos = es.pos;
            tan = es.forward;
            right = es.right;
            return true;
        }
        else
        {
            pos = new Vector3();
            tan = new Vector3();
            right = new Vector3();
            return false;
        }
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
        m_sysClk = new ClockStaticAln();
        m_sysClk.StartClock();
        //initialize for sender
        string strSelfIp = string.Format("{0}.{1}.{2}.{3}", seg[0], seg[1], seg[2], seg[3]);
        DisExerciseConnectionInitializer sInit = new DisExerciseConnectionInitializer();
        sInit.disVersion = 7;
        sInit.useAbsoluteTimestamps = (s_disConf.useAbsStamp); //0:DtTimeStampRelative 1:DtTimeStampAbsolute
        sInit.deviceAddress = strSelfIp;
        SetEntityThresholdsMessage thresholder = new SetEntityThresholdsMessage();
        //thresholder.entityId = m_esr.entityId;
        thresholder.translationTheshold = s_disConf.translationThreshold;
        thresholder.rotationThreshold = s_disConf.rotationThreshold;

        m_cnnsOut = new Dictionary<uint, CnnOut>();
        foreach (uint to in senders)
        {
            Byte[] segs = BitConverter.GetBytes(to);
            string strIpout = string.Format("{0}.{1}.{2}.{3}", segs[0], segs[1], segs[2], segs[3]);
            sInit.destinationAddress = strIpout;
            ExerciseConnection cnn = new ExerciseConnection(sInit);
            cnn.clock.setSimTime((double)m_sysClk.GetTickCnt() /(double)1000);

            CnnOut cnnOut = new CnnOut(cnn);
            m_cnnsOut.Add(to, cnnOut);
        }

        //initialize for receiver
        DisExerciseConnectionInitializer rInit = new DisExerciseConnectionInitializer();
        rInit.disVersion = 7;
        rInit.useAbsoluteTimestamps = (s_disConf.useAbsStamp == true); //0:DtTimeStampRelative 1:DtTimeStampAbsolute
        rInit.deviceAddress = strSelfIp;

        m_cnnIn = new ExerciseConnection(rInit);
        m_entitiesIn = new ReflectedEntityList(m_cnnIn);
        m_cnnIn.clock.setSimTime((double)m_sysClk.GetTickCnt() / (double)1000);

        //CCustomPdu::StartListening<CPduExtObj, (DtPduKind)CCustomPdu::ExtObjState>(m_cnnIn, OnReceiveRawPdu, this);
        //no pdu support, thus can't transfer message

        m_self = self;
        m_statesIn = new Dictionary<GlobalId, EntityState>();
        foreach (uint from in receivers)
        {
            GlobalId id_neighbor = new GlobalId(from, 0);
            EntityState state = new EntityState();
            m_statesIn.Add(id_neighbor, state);
        }

        CoordinateTransform geoc2topo = new CoordinateTransform();
        double refLatitude = s_disConf.latitude;
        double refLongitude = s_disConf.longitude;
        geoc2topo = CoordinateTransform.geocToTopoTransform(refLatitude, refLongitude);
        m_trans = CoordinateTransform.inverse(geoc2topo);

    }

    public void NetworkUnInitialize()
    {
        //unintialize vr-link connections
        m_cnnsOut.Clear();
        m_statesIn.Clear();
    }

    public void PreDynaCalc()
    {
        //prepare environment for subsequent receiving and sending
        double simTime = (double)m_sysClk.GetTickCnt()/(double)1000;
        //pre calc for sending
        foreach (KeyValuePair<uint, CnnOut> pair in m_cnnsOut)
        {
            ExerciseConnection cnn = pair.Value.cnn;
            cnn.clock.setSimTime(simTime);
            cnn.drainInput(-1.0);
        }
        //pre calc for receiving
        foreach (KeyValuePair<GlobalId, EntityState> pair in m_statesIn)
        {
            EntityState es = pair.Value;
            es.updated = false;
            GlobalId id_global = pair.Key;
            m_statesIn[id_global] = es;
        }

        m_cnnIn.clock.setSimTime(simTime);
        m_cnnIn.drainInput(-1.0);
        foreach (ReflectedEntity ent in m_entitiesIn.entities)
        {
            EntityStateRepository esr = ent.esr;
            GlobalId id_global = VrlinkId2GlobalId(esr.entityId);
            EntityState es;
            if (!m_statesIn.TryGetValue(id_global, out es))
                continue;

            esr.algorithm = s_disConf.drAlgor;
            esr.useSmoother(s_disConf.smoothOn);
            Vector3d pos = m_trans.coordTrans(esr.worldPosition);
            TaitBryan eulor = m_trans.eulerTrans(esr.worldOrientation);
            Vector3 tan, right, up;
            Eulor2Frame(eulor, out tan, out right, out up);
            es.pos = new Vector3((float)pos.X, (float)pos.Y, (float)pos.Z);
            es.forward = tan;
            es.right = right;
            es.updated = true;
            m_statesIn[id_global] = es; //fixme: confirm if this line is necessary
        }
    }

    public void PostDynaCalc()
    {
        //wrap up for network communication perframe
        foreach (KeyValuePair<uint, CnnOut> pair in m_cnnsOut)
        {
            CnnOut cnnout = pair.Value;
            foreach (KeyValuePair<ushort, EntityPub> pairInn in cnnout.pubs)
            {
                EntityPublisher pub = pairInn.Value.pub;
                pub.tick(-1.0);
            }
        }
    }

    virtual public void CreateAdoStub(GlobalId id_global, string name, Vector3 pos, Vector3 forward, Vector3 right)
    {

    }

    virtual public void DeleteAdoStub(GlobalId id_global)
    {

    }


}

