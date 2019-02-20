﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointDumper : MonoBehaviour
{

    // Use this for initialization
    class Template
    {
        public Template(string a_name, int a_firstChild, int a_nextSibling, Transform a_t, int a_type)
        {
            name = a_name;
            i_firstChild = a_firstChild;
            i_nextSibling = a_nextSibling;
            t = a_t;
            type = a_type;
        }
        public string name;
        public int i_firstChild;
        public int i_nextSibling;
        public Transform t;
        public int type;
    };
    readonly int[] m_types = {
    4096,
    4128,
    4160,
    4192,
    4224,
    4256,
    4288,
    4320,
    4352,
    4384,
    4416,
    4448,
    4480,
    4512,
    4544,
    4576,
    4608,
    4640,
    4672,
    4704,
    4736,
    4768,
    4800,
    4832,
    4864,
    4896,
    4928,
    4960,
    4992,
    5024,
    5056,
    5088,
    5120,
    5152,
    5184,
    5216,
    5248
  };
    void Start()
    {
        List<Template> l_temp = new List<Template>();
        Queue<Template> q_t = new Queue<Template>();
        Transform root = GetComponent<Transform>();
        Template n_temp = new Template(root.name, -1, -1, root, -1);
        q_t.Enqueue(n_temp);
        while (q_t.Count > 0)
        {
            Template n_t = q_t.Dequeue();
            n_t.type = m_types[l_temp.Count];
            l_temp.Add(n_t);
            int i_base = l_temp.Count + q_t.Count + 1; //index starting with one
            int i_offset = 0;
            Template precceed = null;
            foreach (Transform t in n_t.t)
            {
                if (null == precceed)
                    n_t.i_firstChild = i_base;
                else
                    precceed.i_nextSibling = i_base + i_offset;
                Template c_t = new Template(t.name, -1, -1, t, -1);
                i_offset++;
                q_t.Enqueue(c_t);
                precceed = c_t;
            }
        }

        string strLog = "";
        for (int i = 0; i < l_temp.Count; i++)
        {
            Template t = l_temp[i];
            Vector3 a = t.t.localEulerAngles;

            string item = string.Format("{0}, {1}, ({2}f, {3}f, {4}f), {5}, {6}\n",
                                      t.name, t.type, a.x.ToString("0.00"), a.y.ToString("0.00"), a.z.ToString("0.00"), t.i_firstChild, t.i_nextSibling);
            strLog += item;
        }
        Debug.Log(strLog);
    }

    // Update is called once per frame
    void Update()
    {
        Transform t_this = GetComponent<Transform>();
        Queue<Transform> q_t = new Queue<Transform>();
        q_t.Enqueue(t_this);
        do
        {
            t_this = q_t.Dequeue();
            Transform t_parent = t_this.parent;
            Matrix4x4 p2w = (null == t_parent) ? Matrix4x4.identity : t_parent.localToWorldMatrix;
            Matrix4x4 l2w = t_this.localToWorldMatrix;
            //Matrix4x4 p2w_inv = p2w.inverse;
            Matrix4x4 p2w_inv = (null == t_parent) ? Matrix4x4.identity : t_parent.worldToLocalMatrix;
            Matrix4x4 l2p = p2w_inv * l2w;
            Matrix4x4 r = new Matrix4x4(l2p.GetColumn(0)
                                    , l2p.GetColumn(1)
                                    , l2p.GetColumn(2)
                                    , new Vector4(0, 0, 0, 1));
            Matrix4x4 t = r.inverse * l2p;
            string strLog = string.Format("\n{0}:", t_this.name);
            strLog += "\nlocal to parent:";
            for (int i_r = 0; i_r < 4; i_r ++)
            {
                string strRow = string.Format("\n\t{0} {1} {2} {3}", l2p[i_r, 0].ToString("0.0000"), l2p[i_r, 1].ToString("0.0000"), l2p[i_r, 2].ToString("0.0000"), l2p[i_r, 3].ToString("0.0000"));
                strLog += strRow;
            }
            strLog += "\nrotation:";
            for (int i_r = 0; i_r < 4; i_r ++)
            {
                string strRow = string.Format("\n\t{0} {1} {2} {3}", r[i_r, 0].ToString("0.0000"), r[i_r, 1].ToString("0.0000"), r[i_r, 2].ToString("0.0000"), r[i_r, 3].ToString("0.0000"));
                strLog += strRow;
            }
            strLog += "\ntranslation:";
            for (int i_r = 0; i_r < 4; i_r ++)
            {
                string strRow = string.Format("\n\t{0} {1} {2} {3}", t[i_r, 0].ToString("0.0000"), t[i_r, 1].ToString("0.0000"), t[i_r, 2].ToString("0.0000"), t[i_r, 3].ToString("0.0000"));
                strLog += strRow;
            }
            Debug.Log(strLog);
            foreach (Transform t_c in t_this)
            {
                q_t.Enqueue(t_c);
            }
        } while(q_t.Count > 0);
    }
}
