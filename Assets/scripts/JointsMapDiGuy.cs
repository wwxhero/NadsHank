using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JointsReduction
{
	public class ArtPart
	{
		public readonly int id;
		public readonly string name;
		public Quaternion q = new Quaternion();
		public Vector3 t = new Vector3();
		public ArtPart(int a_id, string a_name)
		{
			id = a_id;
			name = a_name;
		}
	};

	class DiguyJoint
	{
		public MapNode m_node;
		public ArrayList m_children = new ArrayList();
		private Matrix4x4 m_d2s, m_s2d;
		private Matrix4x4 m_n2r = Matrix4x4.identity;
		private ArtPart r_part = null;
		public Matrix4x4 localToParent { get; private set; }
		public Matrix4x4 parentToLocal { get; private set; }
		public Matrix4x4 localToRoot
		{
			get
			{
				return m_n2r;
			}
		}
		public DiguyJoint(Matrix4x4 local2Parent, ArtPart buf)
		{
			localToParent = local2Parent;
			//fixme: inverse for affine+rotation matrix is optmizable
			parentToLocal = Matrix4x4.Inverse(local2Parent);
			r_part = buf;
		}
		public void Initialize(MapNode n, Matrix4x4 d2s, Matrix4x4 n2r_d)
		{
			m_d2s = d2s;
			m_s2d = d2s.inverse;
			m_n2r = n2r_d;
			m_node = n;
		}

		public void Mt_d()
		{
			Debug.Assert(null != r_part);
			Matrix4x4 deltaM_s = m_node.DeltaM_local();
			Matrix4x4 deltaM_d = m_s2d*deltaM_s*m_d2s;
			Matrix4x4 mt_d = localToParent * deltaM_d;
			Quaternion rot = mt_d.rotation;
			r_part.q.Set(rot.x, rot.y, rot.z, rot.w);
			r_part.t.Set(mt_d.m03, mt_d.m13, mt_d.m23);
		}

		public void Mt_s()
		{
			Matrix4x4 mt_d = new Matrix4x4();
			mt_d.SetTRS(r_part.t, r_part.q, new Vector3(1, 1, 1));
			Matrix4x4 deltaM_d = parentToLocal * mt_d;
			Matrix4x4 deltaM_s = m_d2s * deltaM_d * m_s2d;
			m_node.Mt_s(deltaM_s);

			// string info = string.Format("{0}:{1}=>{2}, {3}=>[{4,6:#.00} {5,6:#.00} {6,6:#.00}]", r_part.name
			//     , r_part.q.ToString(), mt_d.rotation.ToString()
			//     , r_part.t.ToString(), mt_d.m03, mt_d.m13, mt_d.m23);
			// Debug.Log(info);
		}
	};

	class DiguyJointDFT
	{
		public Matrix4x4 localToRoot;
		private int m_iNextChild = 0;
		private DiguyJoint m_node;
		public DiguyJointDFT(DiguyJoint node, Matrix4x4 n2r)
		{
			localToRoot = n2r;
			m_node = node;
		}
		public DiguyJoint nextChild()
		{
			DiguyJoint ret = null;
			if (m_iNextChild < m_node.m_children.Count)
			{
				ret = (DiguyJoint)m_node.m_children[m_iNextChild];
				m_iNextChild++;
			}
			return ret;
		}
	};

	class JointsMapDiGuy : JointsMap
	{
		private const int DFN_NJDIGUY = 16;

		private readonly string[] m_reduJs = new string[DFN_NJDIGUY] {
			  "position"
			, "base"
			, "back"
			, "hip_l"
			, "hip_r"
			, "cervical"
			, "shoulder_l"
			, "shoulder_r"
			, "knee_l"
			, "knee_r"
			, "elbow_l"
			, "elbow_r"
			, "ankle_l"
			, "ankle_r"
			, "wrist_l"
			, "wrist_r"
		};
		private readonly float[,] m_m0d = new float[DFN_NJDIGUY, 16] {
			//position: d2s_r * I
			{
				  0f,  -1f,   0f,   0f
				, 0f,   0f,   1f,   0f
				, 1f,   0f,   0f,   0f
				, 0f,   0f,   0f,   1f
			}
			//base:
			, {
				   1.0000f,  0.0000f,  0.0000f,  0.0005f
				,  0.0000f,  1.0000f,  0.0000f,  0.0006f
				,  0.0000f,  0.0000f,  1.0000f,  0.9742f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
			//back:
			, {
				   1.0000f,  0.0000f,  0.0000f,  0.0000f
				,  0.0000f,  1.0000f,  0.0000f,  0.0000f
				,  0.0000f,  0.0000f,  1.0000f,  0.2254f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
			//hip_l:
			, {
				   1.0000f,  0.0000f,  0.0000f,  0.0000f
				,  0.0000f,  1.0000f,  0.0000f,  0.0857f
				,  0.0000f,  0.0000f,  1.0000f,  0.0000f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
			//hip_r:
			, {
				   1.0000f,  0.0000f,  0.0000f,  0.0000f
				,  0.0000f,  1.0000f,  0.0000f, -0.0857f
				,  0.0000f,  0.0000f,  1.0000f,  0.0000f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
			//cervical:
			, {
				   1.0000f,  0.0000f,  0.0000f,  0.0000f
				,  0.0000f,  1.0000f,  0.0000f,  0.0000f
				,  0.0000f,  0.0000f,  1.0000f,  0.4254f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
			//shoulder_l:
			, {
				   1.0000f,  0.0000f,  0.0000f,  0.0000f
				,  0.0000f,  0.0000f, -1.0000f,  0.1619f
				,  0.0000f,  1.0000f,  0.0000f,  0.2794f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
			//shoulder_r:
			, {
				   1.0000f, -0.0000f,  0.0000f,  0.0000f
				,  0.0000f,  0.0000f,  1.0000f, -0.1619f
				, -0.0000f, -1.0000f,  0.0000f,  0.2794f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
			//knee_l:
			, {
				   1.0000f,  0.0000f,  0.0000f,  0.0000f
				,  0.0000f,  1.0000f,  0.0000f,  0.0000f
				,  0.0000f,  0.0000f,  1.0000f, -0.4540f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
			//knee_r:
			, {
				   1.0000f,  0.0000f,  0.0000f,  0.0000f
				,  0.0000f,  1.0000f,  0.0000f,  0.0000f
				,  0.0000f,  0.0000f,  1.0000f, -0.4540f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
			//elbow_l:
			, {
				   1.0000f,  0.0000f,  0.0000f,  0.0000f
				,  0.0000f,  1.0000f,  0.0000f,  0.0000f
				,  0.0000f,  0.0000f,  1.0000f, -0.3016f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
			//elbow_r:
			, {
				   1.0000f,  0.0000f,  0.0000f,  0.0000f
				,  0.0000f,  1.0000f,  0.0000f,  0.0000f
				,  0.0000f,  0.0000f,  1.0000f, -0.3016f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
			//ankle_l:
			, {
				   1.0000f,  0.0000f,  0.0000f,  0.0000f
				,  0.0000f,  1.0000f,  0.0000f,  0.0000f
				,  0.0000f,  0.0000f,  1.0000f, -0.4318f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
			//ankle_r:
			, {
				   1.0000f,  0.0000f,  0.0000f,  0.0000f
				,  0.0000f,  1.0000f,  0.0000f,  0.0000f
				,  0.0000f,  0.0000f,  1.0000f, -0.4318f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
			//wrist_l:
			, {
				   1.0000f,  0.0000f,  0.0000f,  0.0000f
				,  0.0000f,  1.0000f,  0.0000f,  0.0000f
				,  0.0000f,  0.0000f,  1.0000f, -0.2572f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
			//wrist_r:
			, {
				   1.0000f,  0.0000f,  0.0000f,  0.0000f
				,  0.0000f,  1.0000f,  0.0000f,  0.0000f
				,  0.0000f,  0.0000f,  1.0000f, -0.2572f
				,  0.0000f,  0.0000f,  0.0000f,  1.0000f
			}
		};

		DiguyJoint m_jointRoot;

		public void Initialize(Transform root_s, string[] oriJs, Dictionary<string, ArtPart> name2idPart)
		{
			Debug.Assert(oriJs.Length == DFN_NJDIGUY);
			Transform root_src = root_s.Find(oriJs[0]);
			base.Initialize(root_src, oriJs, m_reduJs);

			Dictionary<string, DiguyJoint> dictJoints = new Dictionary<string, DiguyJoint>();
			for (int i_joint = 0; i_joint < DFN_NJDIGUY; i_joint ++)
			{
				string name = m_reduJs[i_joint];
				ArtPart art = null;
				bool hit = name2idPart.TryGetValue(name, out art);
				Debug.Assert("position" == name || hit);
				DiguyJoint joint = new DiguyJoint(DiguyMatrix(i_joint), art);
				dictJoints[name] = joint;
			}
			//construct diguy joint tree
			Stack<MapNodeDFT> dft = new Stack<MapNodeDFT>();
			MapNodeDFT dftNode = new MapNodeDFT(m_rootOut);
			dft.Push(dftNode);
			DiguyJoint joint_r = dictJoints[m_rootOut.name];
			Matrix4x4 n2r_d = joint_r.localToParent;
			Matrix4x4 r2n_s = m_rootOut.src.worldToLocalMatrix * root_s.localToWorldMatrix;
			Matrix4x4 d2s = r2n_s * n2r_d;
			joint_r.Initialize(m_rootOut, d2s, n2r_d);
			while (dft.Count > 0)
			{
				MapNodeDFT p_nodeDFT = dft.Peek();
				DiguyJoint joint_p = dictJoints[p_nodeDFT.n_this.name];
				MapNode c_node = p_nodeDFT.nextChild();
				if (null != c_node)
				{
					MapNodeDFT c_nodeDFT = new MapNodeDFT(c_node);
					dft.Push(c_nodeDFT);
					DiguyJoint joint_c = dictJoints[c_node.name];
					joint_p.m_children.Add(joint_c);
					n2r_d = joint_p.localToRoot * joint_c.localToParent;
					r2n_s = c_node.src.worldToLocalMatrix * root_s.localToWorldMatrix;
					d2s = r2n_s*n2r_d;
					joint_c.Initialize(c_node, d2s, n2r_d);
				}
				else
					dft.Pop();
			}
			m_jointRoot = joint_r;
		}

		Matrix4x4 DiguyMatrix(int i_joint)
		{
			Matrix4x4 ret = new Matrix4x4
			{
				m00 = m_m0d[i_joint, 0]
				,
				m01 = m_m0d[i_joint, 1]
				,
				m02 = m_m0d[i_joint, 2]
				,
				m03 = m_m0d[i_joint, 3]
				,
				m10 = m_m0d[i_joint, 4]
				,
				m11 = m_m0d[i_joint, 5]
				,
				m12 = m_m0d[i_joint, 6]
				,
				m13 = m_m0d[i_joint, 7]
				,
				m20 = m_m0d[i_joint, 8]
				,
				m21 = m_m0d[i_joint, 9]
				,
				m22 = m_m0d[i_joint, 10]
				,
				m23 = m_m0d[i_joint, 11]
				,
				m30 = m_m0d[i_joint, 12]
				,
				m31 = m_m0d[i_joint, 13]
				,
				m32 = m_m0d[i_joint, 14]
				,
				m33 = m_m0d[i_joint, 15]
			};
			return ret;
		}


		// Update is called once per frame
		public void mapOut()
		{
			Queue<DiguyJoint> queBFT = new Queue<DiguyJoint>();
			queBFT.Enqueue(m_jointRoot);
			while (queBFT.Count > 0)
			{
				DiguyJoint joint_p = queBFT.Dequeue();
				for (int i_child = 0; i_child < joint_p.m_children.Count; i_child++)
				{
					DiguyJoint joint_c = (DiguyJoint)joint_p.m_children[i_child];
					queBFT.Enqueue(joint_c);
					joint_c.Mt_d();
				}
			}
		}

		public void mapIn()
		{
			Queue<DiguyJoint> queBFT = new Queue<DiguyJoint>();
			queBFT.Enqueue(m_jointRoot);
			while (queBFT.Count > 0)
			{
				DiguyJoint joint_p = queBFT.Dequeue();
				for (int i_child = 0; i_child < joint_p.m_children.Count; i_child++)
				{
					DiguyJoint joint_c = (DiguyJoint)joint_p.m_children[i_child];
					queBFT.Enqueue(joint_c);
					joint_c.Mt_s();
				}
			}
		}

	}
}
