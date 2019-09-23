public class StateStringsPed
{
	public static string[] s_shortDesc = {
		  "Initial", "Setting road-crossing scenario", "Road-crossing scenario is set", "Setup trackers", "Trackers are attached"
		  , "Road-crossing", "Teleporting"
	};
	public static string[] s_longDesc = {
		  "Turn on one and only one tracker, put it on ground where the participant is to stand!"
		, "Turn on all 5 trackers, standing in center of tracking area in 'T' posture, and look straight ahead."
		, "Walking to cross the road to make sure the space is enough to reach both sides of road."
		, "Confirm all the trackers are tracking, and then step into the avatar and stand in 'T' posture, confirm that you match the avatar's pose by looking at the mirror, or at your own limbs."
		, "Move around to make sure tracking works good."
		, "You may now cross the road."
		, "Teleporting to a new road crossing site."
	};
}