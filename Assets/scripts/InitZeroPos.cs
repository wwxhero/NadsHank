using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitZeroPos : MonoBehaviour {

	// Use this for initialization
	struct Joint
	{
		public Joint (string name, Vector3 angle)
		{
			m_name = name;
			m_angle = angle;
		}
		public string m_name;
		public Vector3 m_angle;
	};
    void Start()
    {
        Joint [] init = {
              new Joint("Base", new Vector3(0.00f, 0.00f, 0.00f))
            , new Joint("Hips", new Vector3(334.10f, 0.00f, 0.00f))
            , new Joint("LHipJoint", new Vector3(81.55f, 159.84f, 255.87f))
            , new Joint("LowerBack", new Vector3(32.82f, 0.00f, 0.00f))
            , new Joint("RHipJoint", new Vector3(81.55f, 200.16f, 104.13f))
            , new Joint("LeftUpLeg", new Vector3(-6.28f, -103.48f, -85f))
            , new Joint("Spine", new Vector3(348.98f, 0.00f, 0.00f))
            , new Joint("RightUpLeg", new Vector3(-6.28f, 103.48f, 85f))
            , new Joint("LeftLeg", new Vector3(6.78f, 0.68f, 359.35f))
            , new Joint("Spine1", new Vector3(11.85f, 0.00f, 0.00f))
            , new Joint("RightLeg", new Vector3(6.78f, 359.32f, 0.65f))
            , new Joint("LeftFoot", new Vector3(287.21f, 0.00f, 4.10f))
            , new Joint("LeftShoulder", new Vector3(348.60f, 358.51f, 98.97f))
            , new Joint("Neck", new Vector3(13.47f, 0.00f, 0.00f))
            , new Joint("RightShoulder", new Vector3(348.60f, 1.49f, 261.03f))
            , new Joint("RightFoot", new Vector3(287.21f, 0.00f, 355.90f))
            , new Joint("LeftToeBase", new Vector3(348.99f, 0.36f, 0.29f))
            , new Joint("LeftArm", new Vector3(1.18f, -1.28f, 79.2f))
            , new Joint("Neck1", new Vector3(0.00f, 0.00f, 0.00f))
            , new Joint("RightArm", new Vector3(1.18f, 1.28f, -79.2f))
            , new Joint("RightToeBase", new Vector3(348.99f, 359.64f, 359.71f))
            , new Joint("LeftForeArm", new Vector3(2.99f, 0.746f, -2.423f))
            , new Joint("Head", new Vector3(336.09f, 0.00f, 0.00f))
            , new Joint("RightForeArm", new Vector3(2.99f, -0.746f, 2.423f))
            , new Joint("LeftHand", new Vector3(10.92f, 359.35f, 6.48f))
            , new Joint("RightHand", new Vector3(10.92f, 0.65f, 353.52f))
            , new Joint("LeftFingerBase", new Vector3(355.82f, 255.76f, 348.52f))
            , new Joint("LThumb", new Vector3(337.13f, 208.74f, 300.04f))
            , new Joint("RightFingerBase", new Vector3(355.82f, 104.24f, 11.48f))
            , new Joint("RThumb", new Vector3(337.13f, 151.26f, 59.96f))
            , new Joint("LeftHandFinger1", new Vector3(15.73f, 1.35f, 5.86f))
            , new Joint("RightHandFinger1", new Vector3(15.73f, 358.65f, 354.14f))
        };
        Queue<Transform> q_t = new Queue<Transform>();
        q_t.Enqueue(transform);
        int i_pos = 0;
        while (q_t.Count > 0)
        {
            Transform t = q_t.Dequeue();
            Debug.Assert(t.name == init[i_pos].m_name);
            t.localEulerAngles = init[i_pos].m_angle;
            i_pos++;
            foreach (Transform c in t)
            {
                q_t.Enqueue(c);
            }
        }
    }

	// Update is called once per frame
	void Update () {

	}
}
