﻿//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Draws different sized room-scale play areas for targeting content
//
//=============================================================================

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using Valve.VR;

[ExecuteInEditMode, RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class PlayArea : MonoBehaviour
{
	public float borderThickness = 0.15f;
	public float wireframeHeight = 2.0f;
	const float c_elevation = 10.0f;
	public enum Size
	{
		_200x400,
		_300x400,
		_400x300,
		_300x225,
		_200x150
	}
    private float[,] c_sizes =
    {
          { 200f, 400f }
        , { 300f, 400f }
        , { 400f, 300f }
        , { 300f, 225f }
        , { 200f, 150f }
    };

	public Size size;
	public Color color = Color.cyan;

	[HideInInspector]
	public Vector3[] vertices;

	public static bool GetBounds( Size size, float [,] sizes, ref HmdQuad_t pRect )
	{
		try
		{
			// convert to half size in meters (from cm)
			var x = sizes[(int)size, 0] / 200;
			var z = sizes[(int)size, 1] / 200;
			pRect.vCorners0.v0 =  x;
			pRect.vCorners0.v1 =  0;
			pRect.vCorners0.v2 = -z;
			pRect.vCorners1.v0 = -x;
			pRect.vCorners1.v1 =  0;
			pRect.vCorners1.v2 = -z;
			pRect.vCorners2.v0 = -x;
			pRect.vCorners2.v1 =  0;
			pRect.vCorners2.v2 =  z;
			pRect.vCorners3.v0 =  x;
			pRect.vCorners3.v1 =  0;
			pRect.vCorners3.v2 =  z;
			return true;
		}
		catch {}

		return false;
	}

	public void BuildMesh()
	{
		var rect = new HmdQuad_t();
		if ( !GetBounds( size, c_sizes, ref rect ) )
			return;

		var corners = new HmdVector3_t[] { rect.vCorners0, rect.vCorners1, rect.vCorners2, rect.vCorners3 };

		vertices = new Vector3[corners.Length * 2];
		for (int i = 0; i < corners.Length; i++)
		{
			var c = corners[i];
			vertices[i] = new Vector3(c.v0, 0.01f, c.v2);
		}

		if (borderThickness == 0.0f)
		{
			GetComponent<MeshFilter>().mesh = null;
			return;
		}

		for (int i = 0; i < corners.Length; i++)
		{
			int next = (i + 1) % corners.Length;
			int prev = (i + corners.Length - 1) % corners.Length;

			var nextSegment = (vertices[next] - vertices[i]).normalized;
			var prevSegment = (vertices[prev] - vertices[i]).normalized;

			var vert = vertices[i];
			vert += Vector3.Cross(nextSegment, Vector3.up) * borderThickness;
			vert += Vector3.Cross(prevSegment, Vector3.down) * borderThickness;

			vertices[corners.Length + i] = vert;
		}

		var triangles = new int[]
		{
			0, 4, 1,
			1, 4, 5,
			1, 5, 2,
			2, 5, 6,
			2, 6, 3,
			3, 6, 7,
			3, 7, 0,
			0, 7, 4
		};

		var uv = new Vector2[]
		{
			new Vector2(0.0f, 0.0f),
			new Vector2(1.0f, 0.0f),
			new Vector2(0.0f, 0.0f),
			new Vector2(1.0f, 0.0f),
			new Vector2(0.0f, 1.0f),
			new Vector2(1.0f, 1.0f),
			new Vector2(0.0f, 1.0f),
			new Vector2(1.0f, 1.0f)
		};

		var colors = new Color[]
		{
			color,
			color,
			color,
			color,
			new Color(color.r, color.g, color.b, 0.0f),
			new Color(color.r, color.g, color.b, 0.0f),
			new Color(color.r, color.g, color.b, 0.0f),
			new Color(color.r, color.g, color.b, 0.0f)
		};

		var mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.colors = colors;
		mesh.triangles = triangles;

		var renderer = GetComponent<MeshRenderer>();
		renderer.material = new Material(Shader.Find("Sprites/Default"));
		renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
		renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		renderer.receiveShadows = false;
		renderer.lightProbeUsage = LightProbeUsage.Off;
	}

	void Start ()
	{
        Vector3 pos_w = transform.position;
        pos_w.y += c_elevation;
        transform.position = pos_w;
		BuildMesh();
	}

	public void Teleport(Matrix4x4 t)
	{
		Vector3 p = transform.position;
        transform.position = t.MultiplyPoint3x4(p);
        transform.rotation = t.GetRotation() * transform.rotation;
	}
}

