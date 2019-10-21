using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasMgr : MonoBehaviour {
	RawImage m_refImg;
	Image m_refScrollImg;
	Image m_refPanelPerson;
	InputField m_refH_f;
	InputField m_refH_i;
	InputField m_refW;
	public bool m_logging = true;
	private const string c_nameProtraitView = "ProtraitView";
	private const string c_nameScrollView = "ScrollView";
	private const string c_namePanelPerson = "PanelPerson";
	private const string c_nameFieldH_f = "PanelPerson/InputFieldH_f";
	private const string c_nameFieldH_i = "PanelPerson/InputFieldH_i";
	private const string c_nameFieldW = "PanelPerson/InputFieldW";
	// Use this for initialization
	void Start () {
		m_refImg = transform.Find(c_nameProtraitView).GetComponent<RawImage>();
		m_refScrollImg = transform.Find(c_nameScrollView).GetComponent<Image>();
		m_refPanelPerson = transform.Find(c_namePanelPerson).GetComponent<Image>();
		m_refH_f = transform.Find(c_nameFieldH_f).GetComponent<InputField>();
		m_refH_i = transform.Find(c_nameFieldH_i).GetComponent<InputField>();
		m_refW = transform.Find(c_nameFieldW).GetComponent<InputField>();
		viewInspec();
		enableInputField(false);
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

	void enableInputField(bool enable)
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

	public void UpdateData(ScenarioControl.ConfAvatar conf, bool save)
	{
		if (save)
		{
			conf.WingSpan = float.Parse(m_refW.text);
			float h_feet = float.Parse(m_refH_f.text);
			float h_inch = float.Parse(m_refH_i.text);
			const float c_feet2meters = 0.3048f;
			const float c_inch2meters = 0.0254f;
			conf.Height = h_feet * c_feet2meters + h_inch * c_inch2meters;
			enableInputField(false);
		}
		else
		{
			enableInputField(true);
			const float c_meter2feet = 3.28084f;
			const float c_foot2inches = 12f;
			float h_feet = conf.Height * c_meter2feet;
			float h_feet_i = Mathf.FloorToInt(h_feet);
			float h_inch = (h_feet - h_feet_i) * c_foot2inches;
			m_refH_f.text = h_feet_i.ToString();
			m_refH_i.text = string.Format("{0,4:#.0}", h_inch);
			m_refW.text = string.Format("{0,4:#.00}", conf.WingSpan);
		}
	}
}
