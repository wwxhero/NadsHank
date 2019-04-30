using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JointsReduction
{
	class MapNode
	{
		private static bool DFN_DBGLOG = false;
		public string name;
		public Transform src;
		public MapNode parent;
		public ArrayList children = new ArrayList();
		private Matrix4x4 m0; //remark: m0 and m0Inv are respected to different basis
		private Matrix4x4 m0Inv;
		private Matrix4x4 m0InvCmp;

		public MapNode(Transform a_src, string a_name, MapNode a_parent)
		{
			src = a_src;
			name = a_name;
			parent = a_parent;
			if (null == parent)
			{
				m0 = Matrix4x4.identity;
				m0Inv = Matrix4x4.identity;
				m0InvCmp = Matrix4x4.identity;
			}
			else
			{
				//for (Transform p = src.parent; p != parent.src; p = p.parent)
				//{
				//	p.localRotation = new Quaternion(0, 0, 0, 1);
				//}
				m0 = src.parent.worldToLocalMatrix * src.localToWorldMatrix;
				m0Inv = src.worldToLocalMatrix * parent.src.localToWorldMatrix;
				m0InvCmp = src.worldToLocalMatrix * src.parent.localToWorldMatrix;
			}


			if (DFN_DBGLOG && null != a_parent)
			{
				string logStr = string.Format("{0}=>{1}\n{2}=>{3}", name, parent.name, src.name, parent.src.name);
				Matrix4x4 m0 = parent.src.worldToLocalMatrix * src.localToWorldMatrix;
				Quaternion r = m0.rotation;
				Vector3 t = new Vector3(m0.m03, m0.m13, m0.m23);
				logStr += string.Format("\n\tr:{0,5:#.00}\t{1,5:#.00}\t{2,5:#.00}" +
										"\n\tt:{3,5:#.00}\t{4,5:#.00}\t{5,5:#.00}"
										, r.eulerAngles.x, r.eulerAngles.y, r.eulerAngles.z, t.x, t.y, t.z);
				//Debug.Log(logStr);
			}

		}

		public Matrix4x4 DeltaM_local()
		{
			if (null == parent)
				return Matrix4x4.identity;
			else
			{
				Matrix4x4 mt = parent.src.worldToLocalMatrix * src.localToWorldMatrix;
				if (DFN_DBGLOG)
				{
					Quaternion r = mt.rotation;
					Vector3 t = new Vector3(mt.m03, mt.m13, mt.m23);
					string logStr = string.Format("{0}=>{1}", name, parent.name);
					logStr += string.Format("\nm(t):{0}=>{1}:" +
														"\n\tr:{2,5:#.00}\t{3,5:#.00}\t{4,5:#.00}\t{5,5:#.00}" +
														"\n\tt:{6,5:#.00}\t{7,5:#.00}\t{8,5:#.00}"
														,src.name, parent.src.name, r.w, r.x, r.y, r.z, t.x, t.y, t.z);

					Quaternion r0 = m0Inv.rotation;
					Vector3 t0 = new Vector3(m0Inv.m03, m0Inv.m13, m0Inv.m23);
					logStr += string.Format("\nm(0)Inv(0):{0}=>{1}:" +
														"\n\tr:{2,5:#.00}\t{3,5:#.00}\t{4,5:#.00}\t{5,5:#.00}" +
														"\n\tt:{6,5:#.00}\t{7,5:#.00}\t{8,5:#.00}"
														,src.name, parent.src.name, r0.w, r0.x, r0.y, r0.z, t0.x, t0.y, t0.z);
					//Debug.Log(logStr);
				}
				return m0Inv*mt;
			}
		}

		public Matrix4x4 DeltaM_localCmp()
		{
			if (null == parent)
				return Matrix4x4.identity;
			else
			{
				Matrix4x4 mt = src.parent.worldToLocalMatrix * src.localToWorldMatrix;
				return m0InvCmp*mt;
			}
		}

		public void Mt_s(Matrix4x4 deltaM_s)
		{
			Matrix4x4 mt_s = m0 * deltaM_s;
			src.localRotation = mt_s.rotation;
			src.localPosition= new Vector3(mt_s.m03, mt_s.m13, mt_s.m23);
			if (DFN_DBGLOG)
			{
				string strInfo = string.Format("{0}:[{1}] [{2,6:#.00} {3,6:#.00} {4,6:#.00}<=({5,6:#.00} {6,6:#.00} {7,6:#.00})]"
					, src.name, mt_s.rotation.eulerAngles.ToString(), src.localPosition.x, src.localPosition.y, src.localPosition.z, mt_s.m03, mt_s.m13, mt_s.m23);
				Debug.Log(strInfo);
			}
		}
	};

	class JointsMap
	{
		public static bool DFN_DBGLOG = false;
		protected MapNode m_rootOut;
		class TransNodeDFT
		{
			public Transform node_this;
			public int i_nextchild;
			public TransNodeDFT(Transform a_tran)
			{
				i_nextchild = 0;
				node_this = a_tran;
			}
			public Transform nextChild()
			{
				Transform ret = null;
				if (i_nextchild < node_this.childCount)
					ret = node_this.GetChild(i_nextchild);
				i_nextchild++;
				return ret;
			}
		};

		public class MapNodeDFT
		{
			public MapNode n_this;
			public int i_nextchild;
			public MapNodeDFT(MapNode a_this)
			{
				i_nextchild = 0;
				n_this = a_this;
			}
			public MapNode nextChild()
			{
				MapNode child = null;
				if (i_nextchild < n_this.children.Count)
					child = (MapNode)n_this.children[i_nextchild];
				i_nextchild++;
				return child;
			}
		};

		public void Initialize(Transform root, string[] j_ori, string[] j_red)
		{
			Debug.Assert(j_ori.Length == j_red.Length);
			Dictionary<string, string> Ori2Redu = new Dictionary<string, string>();
			Dictionary<string, string> Redu2Ori = new Dictionary<string, string>();
			for (int i_map = 0; i_map < j_ori.Length; i_map ++)
			{
				string ori = j_ori[i_map];
				string red = j_red[i_map];
				Ori2Redu[ori] = red;
				Redu2Ori[red] = ori;
			}
			string name_c;
			bool verify = Ori2Redu.TryGetValue(root.name, out name_c);
			Debug.Assert(verify);
			Stack<MapNode> dfcSt = new Stack<MapNode>();
			MapNode n_dfc = new MapNode(root, name_c, null);
			dfcSt.Push(n_dfc);

			//depth first traversing the joint tree
			Stack<TransNodeDFT> dftSt = new Stack<TransNodeDFT>();
			TransNodeDFT n_dft = new TransNodeDFT(root);
			dftSt.Push(n_dft);


			string logSrc = null;
			if (DFN_DBGLOG)
				logSrc = string.Format("{0}\n", n_dft.node_this.name);
			while (dftSt.Count > 0)
			{
				Debug.Assert(dfcSt.Count > 0);
				TransNodeDFT p_node = dftSt.Peek();
				Transform c_tran = p_node.nextChild();
				if (null != c_tran)
				{
					if (DFN_DBGLOG)
					{
						for (int c_indent = 0; c_indent < dftSt.Count; c_indent++)
							logSrc += "\t";
						logSrc += string.Format("{0}\n", c_tran.name);
					}
					TransNodeDFT c_node = new TransNodeDFT(c_tran);
					dftSt.Push(c_node);

					if (Ori2Redu.TryGetValue(c_tran.name, out name_c))
					{
						MapNode p = dfcSt.Peek();
						MapNode c = new MapNode(c_tran, name_c, p);
						p.children.Add(c);
						dfcSt.Push(c);
					}
				}
				else
				{
					dftSt.Pop();
					if (Ori2Redu.TryGetValue(p_node.node_this.name, out name_c))
						dfcSt.Pop();
				}
			}

			m_rootOut = n_dfc;
			if (DFN_DBGLOG)
			{
				Debug.Log(logSrc);
				Stack<MapNodeDFT> dft = new Stack<MapNodeDFT>();
				MapNodeDFT dftNode = new MapNodeDFT(m_rootOut);
				dft.Push(dftNode);
				logSrc = string.Format("{0}\n", m_rootOut.name);
				while (dft.Count > 0)
				{
					MapNodeDFT p_nodeDFT = dft.Peek();
					MapNode c_node = p_nodeDFT.nextChild();
					if (null != c_node)
					{
						for (int i = 0; i < dft.Count; i ++)
							logSrc += "\t";
						logSrc += string.Format("{0}=>{1}\n", c_node.src.name, c_node.name);
						MapNodeDFT c_nodeDFT = new MapNodeDFT(c_node);
						dft.Push(c_nodeDFT);
					}
					else
						dft.Pop();
				}
				Debug.Log(logSrc);
			}
		}
	};
}
