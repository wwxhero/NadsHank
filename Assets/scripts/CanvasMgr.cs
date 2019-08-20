using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasMgr : MonoBehaviour {
	public RawImage m_refImg;
	public bool m_logging = true;
	// Use this for initialization
	void Start () {
        RectTransform this_rct = GetComponent<RectTransform>();
        RectTransform view_rct = m_refImg.GetComponent<RectTransform>();
        float this_w = Mathf.Min(this_rct.rect.width, this_rct.rect.height);
        view_rct.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, this_w);
        view_rct.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, this_w);
        view_rct.anchoredPosition = new Vector2(this_rct.rect.width * 0.5f, -this_rct.rect.height * 0.5f);
    }

	// Update is called once per frame
	void Update () {
		if (m_logging)
		{
			RectTransform rtf = m_refImg.rectTransform;
			string strInfo = string.Format("\nRawImg:\n\tanchoredPosition:{0}\n\tanchorMin:{1}\n\tanchorMax:{2}\n\toffsetMin:{3}\n\toffsetMax:{4}\n\tpivot:{5}\n\trect:{6}"
										, rtf.anchoredPosition.ToString()   //0
										, rtf.anchorMin.ToString()			//1
										, rtf.anchorMax.ToString()			//2
										, rtf.offsetMin.ToString()			//3
										, rtf.offsetMax.ToString()			//4
										, rtf.pivot.ToString()				//5
										, rtf.rect.ToString());				//6
			Debug.Log(strInfo);
		}
	}
}
