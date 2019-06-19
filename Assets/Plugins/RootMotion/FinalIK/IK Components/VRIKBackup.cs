using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RootMotion.FinalIK {
	public class VRIKBackup : MonoBehaviour
	{
		Transform solver_spine_headTarget;
		Transform solver_spine_pelvisTarget;
		float solver_spine_pelvisPositionWeight;
		float solver_spine_pelvisRotationWeight;
		bool solver_plantFeet;
		float solver_spine_maxRootAngle;
		Transform solver_leftArm_target;
		float solver_leftArm_positionWeight;
		float solver_leftArm_rotationWeight;
		Transform solver_rightArm_target;
		float solver_rightArm_positionWeight;
		float solver_rightArm_rotationWeight;
		Transform solver_leftLeg_target;
		float solver_leftLeg_positionWeight;
		float solver_leftLeg_rotationWeight;
		Transform solver_leftLeg_bendGoal;
		float solver_leftLeg_bendGoalWeight;
		Transform solver_rightLeg_target;
		float solver_rightLeg_positionWeight;
		float solver_rightLeg_rotationWeight;
		Transform solver_rightLeg_bendGoal;
		float solver_rightLeg_bendGoalWeight;
		float solver_spine_minHeadHeight;
		float solver_locomotion_weight;
		class NodeJoint
		{
			public Vector3 pos;
			public Quaternion ori;
			public ArrayList children = new ArrayList();
			public Transform tran;
			public NodeJoint(Transform t)
			{
				pos = t.localPosition;
				ori = t.localRotation;
				tran = t;
			}
			public void Reset()
			{
				tran.localPosition = pos;
				tran.localRotation = ori;
			}
		};

		NodeJoint m_root;
		public void Save(VRIK ik)
		{
			IKSolverVR solver = ik.solver;
			solver_spine_headTarget = solver.spine.headTarget;
			solver_spine_pelvisTarget = solver.spine.pelvisTarget;
			solver_spine_pelvisPositionWeight = solver.spine.pelvisPositionWeight;
			solver_spine_pelvisRotationWeight = solver.spine.pelvisRotationWeight;
			solver_plantFeet = solver.plantFeet;
			solver_spine_maxRootAngle = solver.spine.maxRootAngle;
			solver_leftArm_target = solver.leftArm.target;
			solver_leftArm_positionWeight = solver.leftArm.positionWeight;
			solver_leftArm_rotationWeight = solver.leftArm.rotationWeight;
			solver_rightArm_target = solver.rightArm.target;
			solver_rightArm_positionWeight = solver.rightArm.positionWeight;
			solver_rightArm_rotationWeight = solver.rightArm.rotationWeight;
			solver_leftLeg_target = solver.leftLeg.target;
			solver_leftLeg_positionWeight = solver.leftLeg.positionWeight;
			solver_leftLeg_rotationWeight = solver.leftLeg.rotationWeight;
			solver_leftLeg_bendGoal = solver.leftLeg.bendGoal;
			solver_leftLeg_bendGoalWeight = solver.leftLeg.bendGoalWeight;
			solver_rightLeg_target = solver.rightLeg.target;
			solver_rightLeg_positionWeight = solver.rightLeg.positionWeight;
			solver_rightLeg_rotationWeight = solver.rightLeg.rotationWeight;
			solver_rightLeg_bendGoal = solver.rightLeg.bendGoal;
			solver_rightLeg_bendGoalWeight = solver.rightLeg.bendGoalWeight;
			solver_spine_minHeadHeight = solver.spine.minHeadHeight;
			solver_locomotion_weight = solver.locomotion.weight;
            ik.enabled = true;
			ConstructJtree(ik.references.root);
		}

		public void Restore(VRIK ik)
		{
            Debug.Assert(null != m_root);
            ik.enabled = false;
            FreeJtree();
            IKSolverVR solver = ik.solver;
            solver.spine.headTarget = solver_spine_headTarget;
			solver.spine.pelvisTarget = solver_spine_pelvisTarget;
			solver.spine.pelvisPositionWeight = solver_spine_pelvisPositionWeight;
			solver.spine.pelvisRotationWeight = solver_spine_pelvisRotationWeight;
			solver.plantFeet = solver_plantFeet;
			solver.spine.maxRootAngle = solver_spine_maxRootAngle;
			solver.leftArm.target = solver_leftArm_target;
			solver.leftArm.positionWeight = solver_leftArm_positionWeight;
			solver.leftArm.rotationWeight = solver_leftArm_rotationWeight;
			solver.rightArm.target = solver_rightArm_target;
			solver.rightArm.positionWeight = solver_rightArm_positionWeight;
			solver.rightArm.rotationWeight = solver_rightArm_rotationWeight;
			solver.leftLeg.target = solver_leftLeg_target;
			solver.leftLeg.positionWeight = solver_leftLeg_positionWeight;
			solver.leftLeg.rotationWeight = solver_leftLeg_rotationWeight;
			solver.leftLeg.bendGoal = solver_leftLeg_bendGoal;
			solver.leftLeg.bendGoalWeight = solver_leftLeg_bendGoalWeight;
			solver.rightLeg.target = solver_rightLeg_target;
			solver.rightLeg.positionWeight = solver_rightLeg_positionWeight;
			solver.rightLeg.rotationWeight = solver_rightLeg_rotationWeight;
			solver.rightLeg.bendGoal = solver_rightLeg_bendGoal;
			solver.rightLeg.bendGoalWeight = solver_rightLeg_bendGoalWeight;
			solver.spine.minHeadHeight = solver_spine_minHeadHeight;
			solver.locomotion.weight = solver_locomotion_weight;
        }

		public void ConstructJtree(Transform t_root)
		{
			FreeJtree();
			Queue<NodeJoint> q = new Queue<NodeJoint>();

			m_root = new NodeJoint(t_root);
			q.Enqueue(m_root);
			while (q.Count > 0)
			{
				NodeJoint parent = q.Dequeue();
				foreach (Transform t_child in parent.tran)
				{
					NodeJoint n_child = new NodeJoint(t_child);
					q.Enqueue(n_child);
					parent.children.Add(n_child);
				}
			}
		}

		public void FreeJtree()
		{
			if (null != m_root)
			{
				Queue<NodeJoint> q = new Queue<NodeJoint>();
				m_root.Reset();
				q.Enqueue(m_root);
				m_root = null;
				while (q.Count > 0)
				{
					NodeJoint p = q.Dequeue();
					for (int i_c = 0; i_c < p.children.Count; i_c ++)
					{
						NodeJoint c = (NodeJoint)p.children[i_c];
						c.Reset();
						q.Enqueue(c);
						p.children[i_c] = null;
					}
					p.children = null;
				}
			}
		}
	}
}
