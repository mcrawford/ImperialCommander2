using System.Collections.Generic;
using System.Linq;

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
		if ( eligibility == null )
			return true;

		if ( card == null )
			return false;

		if ( eligibility.attackType.HasValue && card.attackType != eligibility.attackType.Value )
			return false;

		if ( eligibility.attackPoolContains != null && eligibility.attackPoolContains.Length > 0
			&& ( card.attacks == null || !eligibility.attackPoolContains.All( die => card.attacks.Contains( die ) ) ) )
			return false;

		if ( eligibility.groupTraitsAny != null && eligibility.groupTraitsAny.Length > 0
			&& ( card.groupTraits == null || !card.groupTraits.Any( trait => eligibility.groupTraitsAny.Contains( trait ) ) ) )
			return false;

		return true;
	}
}

public class OncePerRoundBonusEffectEligibility
{
	public AttackType? attackType;
	public DiceColor[] attackPoolContains;
	public GroupTraits[] groupTraitsAny;
}
