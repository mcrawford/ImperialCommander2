using System;

namespace Saga
{
	public class Attachment
	{
		public string id;
		public string name;
		public string modification;
		public string bonusInstruction;
		public string[] requiredGroupIDs;
		public GroupTraits[] requiredTraits;
		public GroupTraits[] excludedTraits;
		public bool discardOnDefeat;

		public Attachment()
		{

		}
	}
}
