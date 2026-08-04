using System.Collections.Generic;

namespace Saga
{
	public class GlobalBonusEffect
	{
		public string id;
		public string name;
		public string bonusInstruction;

		public GlobalBonusEffect()
		{

		}
	}

	public class GlobalBonusEffectData
	{
		public List<GlobalBonusEffect> globalBonuses;
	}
}
