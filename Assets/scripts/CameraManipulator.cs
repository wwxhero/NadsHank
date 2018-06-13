using UnityEngine;
using System.Collections;

public class CameraManipulator : MonoBehaviour {

    // Use this for initialization
    public float m_unitM = 0.1f;
    public float m_unitA = 0.1f;
    public enum WayRot {self, world};
    public WayRot m_rotWay = WayRot.self;
    Quaternion m_OrgRot;
    Vector3 m_OrgPos;

    void Start () {
        m_OrgRot = this.transform.rotation;
        m_OrgPos = this.transform.position;
	}

    void MoveVert(int dir)
    {
        float dy = m_unitM * dir;
        transform.position += new Vector3(0, dy, 0);
    }

    void MoveHerv(int dir)
    {
        Vector3 dirTL = new Vector3(0, 0, 1);
        Matrix4x4 l2w = transform.localToWorldMatrix;
        Vector3 dirT = l2w.MultiplyVector(dirTL);
        Debug.Assert(Mathf.Abs(dirT.magnitude - 1) < float.Epsilon);
        Vector3 dirTxz = new Vector3(dirT.x, 0, dirT.z);
        dirTxz = dirTxz.normalized;
       Vector3 vecM = dir * m_unitM * dirTxz;
        transform.position += vecM;

    }

    void RotateY(int dir)
    {
        Vector3 axis = new Vector3(0, 1, 0);
        transform.RotateAround(transform.position, axis, m_unitA*dir);
    }

    void RotateBi(int dir)
    {
        Vector3 dirTL = new Vector3(0, 0, 1);
        Matrix4x4 l2w = transform.localToWorldMatrix;
        Vector3 dirT = l2w.MultiplyVector(dirTL);
        Vector3 dirU = new Vector3(0, 1, 0);
        Vector3 dirBi = Vector3.Cross(dirU, dirT);
        transform.RotateAround(transform.position, dirBi, m_unitA * dir);
    }

	// Update is called once per frame
	void Update () {
        int yDir = 0;
        if (Input.GetKey(KeyCode.KeypadMinus))
            yDir = -1;
        else if (Input.GetKey(KeyCode.KeypadPlus))
            yDir = +1;
        if (0 != yDir)
            MoveVert(yDir);

        int tDir = 0;
        if (Input.GetKey(KeyCode.UpArrow))
            tDir = +1;
        else if(Input.GetKey(KeyCode.DownArrow))
            tDir = -1;
        if (0 != tDir)
            MoveHerv(tDir);

        int aDirY = 0;
        if (Input.GetKey(KeyCode.LeftArrow))
            aDirY = -1;
        else if(Input.GetKey(KeyCode.RightArrow))
            aDirY = +1;
        if (0 != aDirY)
            RotateY(aDirY);

        int aDirBi = 0;
        if (Input.GetKey(KeyCode.Keypad8))
            aDirBi = -1;
        else if (Input.GetKey(KeyCode.Keypad2))
            aDirBi = +1;
        if (0 != aDirBi)
            RotateBi(aDirBi);

        if (Input.GetKey(KeyCode.S))
        {
            transform.position = m_OrgPos;
            transform.rotation = m_OrgRot;
        }



            //Vector3 offset = new Vector3(0, 0, 0);
            //Vector3 axis = new Vector3(0, 0, 0);
            //if (Input.GetKey(KeyCode.Keypad8))
            //    offset.z += m_unitM;
            //else if (Input.GetKey(KeyCode.Keypad2))
            //    offset.z -= m_unitM;
            //else if (Input.GetKey(KeyCode.Keypad4))
            //    offset.x -= m_unitM;
            //else if (Input.GetKey(KeyCode.Keypad6))
            //    offset.x += m_unitM;
            //else if (Input.GetKey(KeyCode.KeypadMinus))
            //    offset.y -= m_unitM;
            //else if (Input.GetKey(KeyCode.KeypadPlus))
            //    offset.y += m_unitM;
            //else if (Input.GetKey(KeyCode.UpArrow))
            //    axis.x -= 1.0f;
            //else if (Input.GetKey(KeyCode.DownArrow))
            //    axis.x += 1.0f;
            //else if (Input.GetKey(KeyCode.LeftArrow))
            //    axis.y -= 1.0f;
            //else if (Input.GetKey(KeyCode.RightArrow))
            //    axis.y += 1.0f;
            //else if (Input.GetKey(KeyCode.S))
            //{
            //    this.transform.position = m_OrgPos;
            //    transform.rotation = m_OrgRot;
            //}


            //this.transform.position += offset;
            //switch (m_rotWay)
            //{
            //    case WayRot.world:
            //        this.transform.RotateAround(this.transform.position, axis, axis.magnitude * m_unitA);
            //        break;
            //    case WayRot.self:
            //        this.transform.Rotate(axis * m_unitA);
            //        break;
            //}

    }


}
