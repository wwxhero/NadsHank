public class StateStringsDrv
{
	public static string[] s_shortDesc = {
		  "Initial", "Setting driving scenario", "Driving scenario is set", "Prepare for encarnation step 1 of 2", "Encarnated"
		  , null, null
		  , "Prepare for encarnation step 2 of 2", "Ready for drive", "Tracking", "Tracking", "Tracking"
	};
	public static string[] s_longDesc = {
		  null
		, "Standing in 'T' posture, look straight to the direction the car heads to."
		, null
		, "Stand in 'T' posture to match the posture of the avatar, confirm matching by looking at the trackers directly or through mirror."
		, "Encarnation is done, move your arms and head to confirm for IK effect."
		, null
		, null
		, "Replace HMD with head tracker, then stand in 'T' posture, look straight forward."
		, "Avatar is in the car."
		, "Inspect avatar driver from right."
		, "Inspect avatar driver from front."
		, "Inspect avatar driver from top."
	};
}