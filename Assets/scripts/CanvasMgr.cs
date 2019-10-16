using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasMgr : MonoBehaviour {
	RawImage m_refImg;
	Image m_refScrollImg;
	Image m_refPanelPerson;
	public bool m_logging = true;
	// Use this for initialization
	void Start () {
		m_refImg = transform.Find("ProtraitView").GetComponent<RawImage>();
		m_refScrollImg = transform.Find("ScrollView").GetComponent<Image>();
		m_refPanelPerson = transform.Find("PanelPerson").GetComponent<Image>();
		viewInspec();
		enableInputField(true);
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

	public void viewInspec()
	{
		Color bk = m_refScrollImg.color;
		bk.a = 1;
		m_refScrollImg.color = bk;
		bk = m_refPanelPerson.color;
		bk.a = 1;
		m_refPanelPerson.color = bk;

		m_refImg.gameObject.SetActive(true);

		RectTransform this_rct = GetComponent<RectTransform>();
		RectTransform spec_rct = m_refImg.rectTransform;
		float spec_w = Mathf.Min(this_rct.rect.width, this_rct.rect.height);
		spec_rct.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, spec_w);
		spec_rct.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, spec_w);
		spec_rct.anchoredPosition = new Vector2(this_rct.rect.width * 0.5f, -this_rct.rect.height * 0.5f);

		RectTransform panel_rct = m_refPanelPerson.rectTransform;
		panel_rct.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, this_rct.rect.width - spec_w);
		panel_rct.anchoredPosition = new Vector2(0, 0);
		float c_HeightInputPanel = panel_rct.rect.height;

		RectTransform scroll_rct = m_refScrollImg.rectTransform;
		scroll_rct.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, this_rct.rect.height - c_HeightInputPanel);
		scroll_rct.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, this_rct.rect.width - spec_w);
		scroll_rct.anchoredPosition = new Vector2(0, 0);
	}
	public void viewHmd()
	{
		Color bk = m_refScrollImg.color;
		bk.a = 0;
		m_refScrollImg.color = bk;
		bk = m_refPanelPerson.color;
		bk.a = 0;
		m_refPanelPerson.color = bk;
		m_refImg.gameObject.SetActive(false);
	}

	public void enableInputField(bool enable)
	{
		float alpha = enable ? 1f : 0f;
		foreach (Transform t_c in m_refPanelPerson.transform)
		{
			Image img = t_c.GetComponent<Image>();
			InputField inf = t_c.GetComponent<InputField>();
			if (null != img
				&& null != inf)
			{
				inf.enabled = enable;
				Color bk = img.color;
				bk.a = alpha;
				img.color = bk;
			}
		}
	}
}
