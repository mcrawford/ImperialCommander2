using System.Collections.Generic;

public class OncePerRoundBonusPool
{
	public List<OncePerRoundBonusEffect> effects;
}

public class OncePerRoundBonusEffect
{
	public string id;
	public string name;
	public string text;
	public OncePerRoundBonusEffectEligibility eligibility;

	public bool IsEligible( DeploymentCard card )
	{
		if ( eligibility == null || !eligibility.attackType.HasValue )
			return true;

		return card != null && card.attackType == eligibility.attackType.Value;
	}
}

public class OncePerRoundBonusEffectEligibility
{
	public AttackType? attackType;
}
