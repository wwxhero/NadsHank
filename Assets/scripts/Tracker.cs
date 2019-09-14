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
	static bool IsLeftHand_4(Tracker t)
	{
		return (2 == t.u_d || 3 == t.u_d)
			&& (0 == t.r_d);
	}
	static bool IsRightHand_4(Tracker t)
	{
		return (2 == t.u_d || 3 == t.u_d)
			&& (3 == t.r_d);
	}
	static bool IsPelvis_4(Tracker t)
	{
		return 1 == t.u_d
			&& (2 == t.r_d || 1 == t.r_d);
	}
	static bool IsHead_4(Tracker t)
	{
		return 0 == t.u_d
			&& (2 == t.r_d || 1 == t.r_d);
	}
	static bool IsRightHand_5Drv(Tracker t)
	{
		return (3 == t.u_d || 4 == t.u_d)
			&& (4 == t.r_d);
	}
	static bool IsLeftHand_5Drv(Tracker t)
	{
		return (3 == t.u_d || 4 == t.u_d)
			&& (0 == t.r_d);
	}
	static bool IsHead_5Drv(Tracker t)
	{
		return (2 == t.u_d);
	}
	static bool IsRightFoot_5Drv(Tracker t)
	{
		return (0 == t.u_d || 1 == t.u_d)
			&& (3 == t.r_d);
	}
	static bool IsLeftFoot_5Drv(Tracker t)
	{
		return (0 == t.u_d || 1 == t.u_d)
			&& (1 == t.r_d || 2 == t.r_d);
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
		Tracker.Predicate[] predicates_5 = new Tracker.Predicate[] {
			Tracker.IsRightFoot_5, Tracker.IsLeftFoot_5, Tracker.IsPelvis_5, Tracker.IsRightHand_5, Tracker.IsLeftHand_5
		};
		return IdentifyTrackers(a_trackers, a_hmd, predicates_5);
	}
	//function: sort the trackers in order of 0:right hand, 1:left hand, 2:head, 3:right foot, 4:left foot
	public static bool IdentifyTrackers_5Drv(GameObject[] a_trackers, Transform a_hmd)
	{
		Tracker.Predicate[] predicates_5 = new Tracker.Predicate[] {
			Tracker.IsRightHand_5Drv, Tracker.IsLeftHand_5Drv, Tracker.IsHead_5Drv, Tracker.IsRightFoot_5Drv, Tracker.IsLeftFoot_5Drv
		};
		return IdentifyTrackers(a_trackers, a_hmd, predicates_5);
	}
	//function: sort the trackers in order of 0:right hand, 1:left hand, 2:pelvis
	public static bool IdentifyTrackers_3(GameObject[] a_trackers, Transform a_hmd)
	{
		Tracker.Predicate[] predicates_3 = new Tracker.Predicate[] {
			Tracker.IsRightHand_3, Tracker.IsLeftHand_3, Tracker.IsPelvis_3
		};
		return IdentifyTrackers(a_trackers, a_hmd, predicates_3);
	}
	//function: sort the trackers in order of 0:right hand, 1:left hand, 2:pelvis, 3:head
	public static bool IdentifyTrackers_4(GameObject[] a_trackers, Transform a_hmd)
	{
		Tracker.Predicate[] predicates_4 = new Tracker.Predicate[] {
			Tracker.IsRightHand_4, Tracker.IsLeftHand_4, Tracker.IsPelvis_4, Tracker.IsHead_4
		};
		return IdentifyTrackers(a_trackers, a_hmd, predicates_4);
	}
	private static bool IdentifyTrackers(GameObject[] a_trackers, Transform a_hmd, Predicate[] a_predicates)
	{
		Debug.Assert(a_trackers.Length == a_predicates.Length);
		int n_tracker = a_trackers.Length;
		Tracker[] trackers = new Tracker[n_tracker];
		List<Tracker> lst_r = new List<Tracker>();
		List<Tracker> lst_u = new List<Tracker>();
		for (int i_tracker = 0; i_tracker < n_tracker; i_tracker++)
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

		int[] id2tracker = new int[n_tracker];
		const int c_unidentified = -1;
		for (int i_predicate = 0; i_predicate < n_tracker; i_predicate ++)
			id2tracker[i_predicate] = c_unidentified;
		for (int i_tracker = 0; i_tracker < trackers.Length; i_tracker++)
		{
			bool identified = false;
			Tracker t = trackers[i_tracker];
			int id = 0;
			while (id < a_predicates.Length)
			{
				identified = a_predicates[id](t);
				if (identified)
					break;
				else
					id++;
			}
			if (!identified)
				break;
			id2tracker[id] = i_tracker;
		}

		bool all_predictates_hit = true;
		for (int i_predicate = 0; i_predicate < id2tracker.Length && all_predictates_hit; i_predicate ++)
			all_predictates_hit = (id2tracker[i_predicate] != c_unidentified);
		if (all_predictates_hit)
		{
			for (int id = 0; id < n_tracker; id ++)
				a_trackers[id] = trackers[id2tracker[id]].tracker;
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
}