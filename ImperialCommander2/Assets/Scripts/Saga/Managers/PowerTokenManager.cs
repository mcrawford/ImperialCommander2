using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Saga
{
	public static class PowerTokenManager
	{
		static Dictionary<string, List<PowerTokenType>> TokenMap
		{
			get
			{
				var vars = DataStore.sagaSessionData?.gameVars;
				if ( vars == null )
					return null;
				if ( vars.handPowerTokens == null )
					vars.handPowerTokens = new Dictionary<string, List<PowerTokenType>>();
				return vars.handPowerTokens;
			}
		}

		public static List<PowerTokenType> GetTokens( string cardId )
		{
			var map = TokenMap;
			if ( map == null || string.IsNullOrEmpty( cardId ) || !map.ContainsKey( cardId ) )
				return new List<PowerTokenType>();
			return map[cardId] ?? new List<PowerTokenType>();
		}

		public static void ClearTokens( string cardId )
		{
			var map = TokenMap;
			if ( map == null || string.IsNullOrEmpty( cardId ) )
				return;
			map.Remove( cardId );
		}

		public static int TokenCapacity( DeploymentCard card )
		{
			if ( card == null )
				return 0;
			return 2 * card.size;
		}

		/// <summary>
		/// Commence Landing + Implacable: at start of each round, distribute
		/// 1 Damage, 1 Block, and 1 Evade among hand cards (evenly, capped at 2× size).
		/// </summary>
		public static void PlaceCommenceLandingTokens()
		{
			var map = TokenMap;
			if ( map == null || DataStore.deploymentHand == null || DataStore.deploymentHand.Count == 0 )
				return;

			PruneOrphanedEntries();

			// Commence Landing
			PlaceOneToken( PowerTokenType.Damage );
			PlaceOneToken( PowerTokenType.Block );
			// Implacable (hand placement only; share/exhaust ability omitted for IC2)
			PlaceOneToken( PowerTokenType.Evade );
		}

		// === Personal Flagship (always-on OO) ===

		public const string PersonalFlagshipVillainId = "DG084"; // Maul
		public const string PersonalFlagshipEffect =
			"PERSONAL FLAGSHIP: A friendly figure within 3 of this Villain gains 1 {h} or 1 {g}.";

		public static bool IsPersonalFlagshipEffect( string effect )
		{
			return !string.IsNullOrEmpty( effect ) &&
				effect.StartsWith( "PERSONAL FLAGSHIP:", System.StringComparison.OrdinalIgnoreCase );
		}

		/// <summary>
		/// Personal Flagship: after open groups are chosen, always include Maul in the hand.
		/// </summary>
		public static void EnsurePersonalFlagshipVillainInHand()
		{
			EnsureVillainInHand( PersonalFlagshipVillainId, "Personal Flagship" );
		}

		static void EnsureVillainInHand( string villainId, string reason )
		{
			var villain = DataStore.GetEnemy( villainId );
			if ( villain == null )
			{
				Debug.LogWarning( $"EnsureVillainInHand()::{villainId} not found ({reason})" );
				return;
			}

			if ( DataStore.deploymentHand.ContainsCard( villain ) )
				return;

			if ( DataStore.deployedEnemies.ContainsCard( villain ) )
				return;

			// Mission-reserved villains stay reserved (do not duplicate into open groups)
			if ( DataStore.sagaSessionData.MissionReserved.ContainsCard( villain ) )
				return;

			DataStore.deploymentHand.Add( villain );
			DataStore.manualDeploymentList.Remove( villain );
			Debug.Log( $"{reason}: added {villain.name} ({villain.id}) to deployment hand" );
		}

		/// <summary>
		/// Ensure the Personal Flagship once-per-round pool effect is available.
		/// </summary>
		public static void EnsurePersonalFlagshipPoolEffect()
		{
			EnsurePoolEffect( PersonalFlagshipEffect );
		}

		// === Limitless Arsenal (always-on OO) ===

		public const string LimitlessArsenalPoolKey = "LIMITLESS ARSENAL";
		public const string TieFighterPatrolPoolKey = "TIE FIGHTER PATROL";

		public static bool IsLimitlessArsenalEffect( string effect )
		{
			return !string.IsNullOrEmpty( effect ) &&
				effect.StartsWith( LimitlessArsenalPoolKey, System.StringComparison.OrdinalIgnoreCase );
		}

		public static bool IsTieFighterPatrolEffect( string effect )
		{
			return !string.IsNullOrEmpty( effect ) &&
				effect.StartsWith( TieFighterPatrolPoolKey, System.StringComparison.OrdinalIgnoreCase );
		}

		/// <summary>
		/// Ensure the Limitless Arsenal once-per-round pool marker is available.
		/// Expanded to a concrete instruction when assigned.
		/// </summary>
		public static void EnsureLimitlessArsenalPoolEffect()
		{
			EnsurePoolEffect( LimitlessArsenalPoolKey );
		}

		/// <summary>Ensure the TIE Fighter Patrol once-per-round effect is available.</summary>
		public static void EnsureTieFighterPatrolPoolEffect()
		{
			EnsurePoolEffect( TieFighterPatrolPoolKey );
		}

		static void EnsurePoolEffect( string effect )
		{
			if ( DataStore.oncePerRoundBonusPool == null )
				DataStore.oncePerRoundBonusPool = new List<string>();

			if ( DataStore.oncePerRoundBonusPool.Any( e =>
				string.Equals( e, effect, System.StringComparison.OrdinalIgnoreCase ) ||
				( IsLimitlessArsenalEffect( effect ) && IsLimitlessArsenalEffect( e ) ) ||
				( IsTieFighterPatrolEffect( effect ) && IsTieFighterPatrolEffect( e ) ) ||
				( IsPersonalFlagshipEffect( effect ) && IsPersonalFlagshipEffect( e ) ) ) )
				return;

			DataStore.oncePerRoundBonusPool.Add( effect );
			Debug.Log( $"OO pool: added '{effect}'" );
		}

		/// <summary>
		/// True if this activating group has a sensible Limitless Arsenal target in hand.
		/// </summary>
		public static bool CanApplyLimitlessArsenal( DeploymentCard activator )
		{
			return PickLimitlessArsenalSource( activator ) != null;
		}

		/// <summary>
		/// Expand the pool marker into a concrete once-per-round instruction for this activator.
		/// </summary>
		public static string FormatLimitlessArsenalEffect( DeploymentCard activator )
		{
			var source = PickLimitlessArsenalSource( activator );
			if ( source == null )
				return null;

			string dice = FormatAttackPool( source );
			return $"{LimitlessArsenalPoolKey}: Use {source.name}'s attack pool ({dice}) for this figure's attacks this activation.";
		}

		/// <summary>
		/// Hand card with {h}/{b}, attack-type compatible with activator, best by dice/red/cost.
		/// </summary>
		public static DeploymentCard PickLimitlessArsenalSource( DeploymentCard activator )
		{
			if ( activator == null || DataStore.deploymentHand == null )
				return null;

			if ( activator.attackType == AttackType.None ||
				 activator.attacks == null || activator.attacks.Length == 0 )
				return null;

			var eligible = DataStore.deploymentHand
				.Where( c => c != null && !string.IsNullOrEmpty( c.id ) )
				.Where( c => c.attacks != null && c.attacks.Length > 0 && c.attackType != AttackType.None )
				.Where( HasDamageOrSurgeToken )
				.Where( c => IsLimitlessCompatible( activator, c ) )
				// Limitless Arsenal is only useful when the replacement improves
				// on the activator's native attack pool.
				.Where( c => LimitlessScore( activator, c ) > LimitlessScore( activator, activator ) )
				.ToList();

			if ( eligible.Count == 0 )
				return null;

			return eligible
				.OrderByDescending( c => LimitlessScore( activator, c ) )
				.ThenByDescending( c => c.cost )
				.ThenBy( c => c.id )
				.First();
		}

		static bool HasDamageOrSurgeToken( DeploymentCard card )
		{
			var tokens = GetTokens( card.id );
			return tokens.Any( t => t == PowerTokenType.Damage || t == PowerTokenType.Surge );
		}

		/// <summary>
		/// Ranged activators need a ranged pool or at least one Blue (accuracy).
		/// Melee activators accept any pool (scoring prefers melee).
		/// </summary>
		static bool IsLimitlessCompatible( DeploymentCard activator, DeploymentCard source )
		{
			if ( activator.attackType == AttackType.Ranged )
			{
				if ( source.attackType == AttackType.Ranged )
					return true;
				// Allow melee-typed cards only if they somehow include Blue accuracy
				return source.attacks != null && source.attacks.Any( d => d == DiceColor.Blue );
			}

			return true;
		}

		static int LimitlessScore( DeploymentCard activator, DeploymentCard source )
		{
			int score = 0;
			if ( source.attackType == activator.attackType )
				score += 1000;
			score += (source.attacks?.Length ?? 0) * 100;
			score += (source.attacks?.Count( d => d == DiceColor.Red ) ?? 0) * 10;
			if ( activator.attackType == AttackType.Ranged )
				score += (source.attacks?.Count( d => d == DiceColor.Blue ) ?? 0) * 5;
			score += source.cost;
			return score;
		}

		static string FormatAttackPool( DeploymentCard card )
		{
			if ( card?.attacks == null || card.attacks.Length == 0 )
				return "None";
			return string.Join( " ", card.attacks.Select( d => d.ToString() ) );
		}

		static void PlaceOneToken( PowerTokenType token )
		{
			var map = TokenMap;
			var available = DataStore.deploymentHand
				.Where( c => c != null && !string.IsNullOrEmpty( c.id ) )
				.Where( c => GetTokens( c.id ).Count < TokenCapacity( c ) )
				.ToList();

			if ( available.Count == 0 )
				return;

			// Start a random card only when there are no usable token-bearing cards.
			// Otherwise continue building a card that already has power tokens.
			var tokenBearing = available.Where( c => GetTokens( c.id ).Count > 0 ).ToList();
			var candidates = tokenBearing.Count > 0 ? tokenBearing : available;
			int[] randomTarget = GlowEngine.GenerateRandomNumbers( candidates.Count );
			var target = candidates[randomTarget[0]];
			if ( !map.ContainsKey( target.id ) )
				map[target.id] = new List<PowerTokenType>();
			map[target.id].Add( token );
		}

		/// <summary>
		/// Remove token entries for cards no longer in the deployment hand.
		/// </summary>
		public static void PruneOrphanedEntries()
		{
			var map = TokenMap;
			if ( map == null )
				return;

			var handIds = new HashSet<string>(
				DataStore.deploymentHand
					.Where( c => c != null && !string.IsNullOrEmpty( c.id ) )
					.Select( c => c.id ) );

			var orphaned = map.Keys.Where( id => !handIds.Contains( id ) ).ToList();
			foreach ( var id in orphaned )
				map.Remove( id );
		}

		public static string FormatHandSummary( List<PowerTokenType> tokens )
		{
			if ( tokens == null || tokens.Count == 0 )
				return "";

			int dmg = tokens.Count( t => t == PowerTokenType.Damage );
			int blk = tokens.Count( t => t == PowerTokenType.Block );
			int surge = tokens.Count( t => t == PowerTokenType.Surge );
			int evade = tokens.Count( t => t == PowerTokenType.Evade );

			var parts = new List<string>();
			if ( dmg > 0 )
				parts.Add( $"{{h}}×{dmg}" );
			if ( blk > 0 )
				parts.Add( $"{{g}}×{blk}" );
			if ( surge > 0 )
				parts.Add( $"{{b}}×{surge}" );
			if ( evade > 0 )
				parts.Add( $"{{f}}×{evade}" );

			return string.Join( " ", parts );
		}

		public static string FormatTokenName( PowerTokenType token )
		{
			// Glyph codes — TextBox / ReplaceGlyphs render these as Imperial Assault symbols
			return token switch
			{
				PowerTokenType.Damage => "{h}",
				PowerTokenType.Block => "{g}",
				PowerTokenType.Surge => "{b}",
				PowerTokenType.Evade => "{f}",
				_ => token.ToString()
			};
		}

		/// <summary>
		/// Even round-robin across figures (max 2 tokens each). Returns the QuickMessage body.
		/// </summary>
		public static string FormatDeployAssignment( DeploymentCard card, List<PowerTokenType> tokens )
		{
			if ( card == null || tokens == null || tokens.Count == 0 )
				return "";

			int size = System.Math.Max( 1, card.size );
			var perFigure = new List<List<PowerTokenType>>( size );
			for ( int i = 0; i < size; i++ )
				perFigure.Add( new List<PowerTokenType>() );

			int figureIndex = 0;
			foreach ( var token in tokens )
			{
				// Find next figure with room (max 2), wrapping from figureIndex
				bool placed = false;
				for ( int attempt = 0; attempt < size; attempt++ )
				{
					int idx = (figureIndex + attempt) % size;
					if ( perFigure[idx].Count < 2 )
					{
						perFigure[idx].Add( token );
						figureIndex = (idx + 1) % size;
						placed = true;
						break;
					}
				}
				// Card was capped at 2×size on placement, so this should not happen
				if ( !placed )
					break;
			}

			var sb = new StringBuilder();
			sb.Append( "Power tokens — " );
			var segments = new List<string>();
			for ( int i = 0; i < size; i++ )
			{
				if ( perFigure[i].Count == 0 )
					segments.Add( $"Figure {i + 1}: —" );
				else
					segments.Add( $"Figure {i + 1}: {string.Join( ", ", perFigure[i].Select( FormatTokenName ) )}" );
			}
			sb.Append( string.Join( "; ", segments ) );
			return sb.ToString();
		}

		/// <summary>
		/// If difficulty swap changed the card id, move hand tokens onto the final deployed id.
		/// </summary>
		public static void TransferTokens( string fromCardId, string toCardId )
		{
			if ( string.IsNullOrEmpty( fromCardId ) || string.IsNullOrEmpty( toCardId ) || fromCardId == toCardId )
				return;

			var tokens = GetTokens( fromCardId );
			ClearTokens( fromCardId );
			if ( tokens.Count == 0 )
				return;

			var map = TokenMap;
			if ( map == null )
				return;

			if ( !map.ContainsKey( toCardId ) )
				map[toCardId] = new List<PowerTokenType>();
			map[toCardId].AddRange( tokens );
		}

		/// <summary>
		/// Returns a power-token assignment line for the deploy text box, then clears those tokens.
		/// Empty string if none.
		/// </summary>
		public static string ConsumeDeployAppendix( DeploymentCard card )
		{
			if ( card == null || string.IsNullOrEmpty( card.id ) )
				return "";

			var tokens = GetTokens( card.id );
			if ( tokens.Count == 0 )
			{
				ClearTokens( card.id );
				return "";
			}

			string appendix = FormatDeployAssignment( card, tokens );
			ClearTokens( card.id );
			return appendix;
		}
	}
}
