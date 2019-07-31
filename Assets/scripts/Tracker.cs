using UnityEngine;
using System.Collections.Generic;
public class Tracker
{
	GameObject tracker;
	float r, u;
	int r_d, u_d;
	Tracker(GameObject a_tracker, float a_r, float a_u)
	{
		tracker = a_tracker;
		r = a_r;
		u = a_u;
	}
	static int Compare_r(Tracker x, Tracker y)
	{
		float d = x.r - y.r;
		if (d < 0)
			return -1;
		else if (d > 0)
			return +1;
		else
			return 0;
	}
	static int Compare_u(Tracker x, Tracker y)
	{
		float d = x.u - y.u;
		if (d < 0)
			return -1;
		else if (d > 0)
			return +1;
		else
			return 0;
	}
	static bool IsRightFoot_5(Tracker t)
	{
		return (0 == t.u_d || 1 == t.u_d)
			&& (3 == t.r_d || 4 == t.r_d);
	}
	static bool IsLeftFoot_5(Tracker t)
	{
		return (0 == t.u_d || 1 == t.u_d)
			&& (0 == t.r_d || 1 == t.r_d);
	}
	static bool IsPelvis_5(Tracker t)
	{
		return 2 == t.u_d && 2 == t.r_d;
	}
	static bool IsRightHand_5(Tracker t)
	{
		return (3 == t.u_d || 4 == t.u_d)
			&& (3 == t.r_d || 4 == t.r_d);
	}
	static bool IsLeftHand_5(Tracker t)
	{
		return (3 == t.u_d || 4 == t.u_d)
			&& (0 == t.r_d || 1 == t.r_d);
	}
	static bool IsLeftHand_3(Tracker t)
	{
		return (1 == t.u_d || 2 == t.u_d)
			&& (0 == t.r_d);
	}
	static bool IsRightHand_3(Tracker t)
	{
		return (1 == t.u_d || 2 == t.u_d)
			&& (2 == t.r_d);
	}
	static bool IsPelvis_3(Tracker t)
	{
		return 0 == t.u_d
			&& 1 == t.r_d;
	}
	delegate bool Predicate(Tracker t);
	//function: sort the trackers in order of 0:right foot, 1:left foot, 2:pelvis, 3:right hand, 4:left hand
	//parameters:
	//	a_trackers: the trackers are to be identified (sorted)
	//	a_hmd: head mount display
	//return value:
	//	true:success
	public static bool IdentifyTrackers_5(GameObject[] a_trackers, Transform a_hmd)
	{
		Debug.Assert(a_trackers.Length == 5); //supports 5 trackers only
		if (5 != a_trackers.Length)
			return false;
		Tracker[] trackers = new Tracker[5];
		List<Tracker> lst_r = new List<Tracker>();
		List<Tracker> lst_u = new List<Tracker>();
		for (int i_tracker = 0; i_tracker < 5; i_tracker++)
		{
			GameObject o_t = a_trackers[i_tracker];
			if (!o_t.activeSelf)
				return false;
			Vector3 v_t = o_t.transform.position - a_hmd.position;
			float r_t = Vector3.Dot(a_hmd.right, v_t);
			float u_t = Vector3.Dot(a_hmd.up, v_t);
			Tracker t = new Tracker(o_t, r_t, u_t);
			trackers[i_tracker] = t;
			lst_r.Add(t);
			lst_u.Add(t);
		}
		lst_r.Sort(Tracker.Compare_r);
		lst_u.Sort(Tracker.Compare_u);
		List<Tracker>.Enumerator it = lst_r.GetEnumerator();
		bool next = it.MoveNext();
		for (int i_r = 0
			; next && i_r < trackers.Length
			; i_r++, next = it.MoveNext())
		{
			Tracker t = it.Current;
			t.r_d = i_r;
		}
		it = lst_u.GetEnumerator();
		next = it.MoveNext();
		for (int i_u = 0
			; next && i_u < trackers.Length
			; i_u++, next = it.MoveNext())
		{
			Tracker t = it.Current;
			t.u_d = i_u;
		}
		Tracker.Predicate[] predicates = new Tracker.Predicate[] {
			Tracker.IsRightFoot_5, Tracker.IsLeftFoot_5, Tracker.IsPelvis_5, Tracker.IsRightHand_5, Tracker.IsLeftHand_5
		};
		int[] hit_trackers = new int[] {
			-1, -1, -1, -1, -1
		};
		for (int i_tracker = 0; i_tracker < trackers.Length; i_tracker++)
		{
			bool identified = false;
			Tracker t = trackers[i_tracker];
			int id = 0;
			while (id < predicates.Length)
			{
				identified = predicates[id](t);
				if (identified)
					break;
				else
					id++;
			}
			if (!identified)
				break;
			hit_trackers[id] = i_tracker;
		}
		if (hit_trackers[0] > -1
		 && hit_trackers[1] > -1
		 && hit_trackers[2] > -1
		 && hit_trackers[3] > -1
		 && hit_trackers[4] > -1)
		{
			a_trackers[0] = trackers[hit_trackers[0]].tracker;
			a_trackers[1] = trackers[hit_trackers[1]].tracker;
			a_trackers[2] = trackers[hit_trackers[2]].tracker;
			a_trackers[3] = trackers[hit_trackers[3]].tracker;
			a_trackers[4] = trackers[hit_trackers[4]].tracker;
			return true;
		}
		else
			return false;
	}
	//function: sort the trackers in order of 0:right hand, 1:left hand
	public static bool IdentifyTrackers_2(GameObject[] a_trackers, Transform a_hmd)
	{
		float proj_r_0 = Vector3.Dot(a_trackers[0].transform.position - a_hmd.position, a_hmd.right);
		float proj_r_1 = Vector3.Dot(a_trackers[1].transform.position - a_hmd.position, a_hmd.right);
		bool is_0_onright = (proj_r_0 > 0);
		bool is_1_onright = (proj_r_1 > 0);
		if (is_0_onright != is_1_onright)
		{
			if (!is_0_onright)
			{
				GameObject temp = a_trackers[0];
				a_trackers[0] = a_trackers[1];
				a_trackers[1] = temp;
			}
			return true;
		}
		else
			return false;
	}
	//function: sort the trackers in order of 0:right hand, 1:left hand, 2:pelvis
	//fixme: combine IdentifyTrackers_5 in same logic
	public static bool IdentifyTrackers_3(GameObject[] a_trackers, Transform a_hmd)
	{
		Debug.Assert(a_trackers.Length == 3); //supports 5 trackers only
		if (3 != a_trackers.Length)
			return false;
		Tracker[] trackers = new Tracker[3];
		List<Tracker> lst_r = new List<Tracker>();
		List<Tracker> lst_u = new List<Tracker>();
		for (int i_tracker = 0; i_tracker < 3; i_tracker++)
		{
			GameObject o_t = a_trackers[i_tracker];
			if (!o_t.activeSelf)
				return false;
			Vector3 v_t = o_t.transform.position - a_hmd.position;
			float r_t = Vector3.Dot(a_hmd.right, v_t);
			float u_t = Vector3.Dot(a_hmd.up, v_t);
			Tracker t = new Tracker(o_t, r_t, u_t);
			trackers[i_tracker] = t;
			lst_r.Add(t);
			lst_u.Add(t);
		}
		lst_r.Sort(Tracker.Compare_r);
		lst_u.Sort(Tracker.Compare_u);
		List<Tracker>.Enumerator it = lst_r.GetEnumerator();
		bool next = it.MoveNext();
		for (int i_r = 0
			; next && i_r < trackers.Length
			; i_r++, next = it.MoveNext())
		{
			Tracker t = it.Current;
			t.r_d = i_r;
		}
		it = lst_u.GetEnumerator();
		next = it.MoveNext();
		for (int i_u = 0
			; next && i_u < trackers.Length
			; i_u++, next = it.MoveNext())
		{
			Tracker t = it.Current;
			t.u_d = i_u;
		}
		Tracker.Predicate[] predicates = new Tracker.Predicate[] {
			Tracker.IsRightHand_3, Tracker.IsLeftHand_3, Tracker.IsPelvis_3
		};
		int[] hit_trackers = new int[] {
			-1, -1, -1
		};
		for (int i_tracker = 0; i_tracker < trackers.Length; i_tracker++)
		{
			bool identified = false;
			Tracker t = trackers[i_tracker];
			int id = 0;
			while (id < predicates.Length)
			{
				identified = predicates[id](t);
				if (identified)
					break;
				else
					id++;
			}
			if (!identified)
				break;
			hit_trackers[id] = i_tracker;
		}
		if (hit_trackers[0] > -1
		 && hit_trackers[1] > -1
		 && hit_trackers[2] > -1)
		{
			a_trackers[0] = trackers[hit_trackers[0]].tracker;
			a_trackers[1] = trackers[hit_trackers[1]].tracker;
			a_trackers[2] = trackers[hit_trackers[2]].tracker;
			return true;
		}
		else
			return false;
	}
}