using System;

namespace Saga
{
	public class Attachment
	{
		public string id;
		public string name;
		public string modification;
		public string bonusInstruction;
		public GroupTraits[] excludedTraits;
		/// <summary>Mission IDs this attachment applies to; empty means every mission.</summary>
		public string[] missionIDs;
		/// <summary>Eligible deployment-card footprint values; empty means every size.</summary>
		public string[] allowedMiniSizes;
		/// <summary>Discard instead of making this attachment available after its group is defeated.</summary>
		public bool discardWhenGroupDefeated;

		public Attachment()
		{

		}
	}
}
