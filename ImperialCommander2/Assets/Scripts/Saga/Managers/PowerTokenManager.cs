using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		/// Commence Landing: at start of each round, distribute 1 Damage and 1 Block
		/// among hand cards, spreading evenly and capping each card at 2× size.
		/// </summary>
		public static void PlaceCommenceLandingTokens()
		{
			var map = TokenMap;
			if ( map == null || DataStore.deploymentHand == null || DataStore.deploymentHand.Count == 0 )
				return;

			PruneOrphanedEntries();

			PlaceOneToken( PowerTokenType.Damage );
			PlaceOneToken( PowerTokenType.Block );
		}

		static void PlaceOneToken( PowerTokenType token )
		{
			var map = TokenMap;
			var candidates = DataStore.deploymentHand
				.Where( c => c != null && !string.IsNullOrEmpty( c.id ) )
				.Where( c => GetTokens( c.id ).Count < TokenCapacity( c ) )
				.OrderBy( c => GetTokens( c.id ).Count )
				.ThenBy( c => DataStore.deploymentHand.IndexOf( c ) )
				.ToList();

			if ( candidates.Count == 0 )
				return;

			var target = candidates[0];
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
				parts.Add( $"Dmg×{dmg}" );
			if ( blk > 0 )
				parts.Add( $"Blk×{blk}" );
			if ( surge > 0 )
				parts.Add( $"Srg×{surge}" );
			if ( evade > 0 )
				parts.Add( $"Evd×{evade}" );

			return string.Join( " ", parts );
		}

		public static string FormatTokenName( PowerTokenType token )
		{
			return token switch
			{
				PowerTokenType.Damage => "Damage",
				PowerTokenType.Block => "Block",
				PowerTokenType.Surge => "Surge",
				PowerTokenType.Evade => "Evade",
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
			sb.Append( $"{card.name} deploy with power tokens — " );
			var segments = new List<string>();
			for ( int i = 0; i < size; i++ )
			{
				if ( perFigure[i].Count == 0 )
					segments.Add( $"F{i + 1}: —" );
				else
					segments.Add( $"F{i + 1}: {string.Join( ", ", perFigure[i].Select( FormatTokenName ) )}" );
			}
			sb.Append( string.Join( "; ", segments ) );
			return sb.ToString();
		}

		/// <summary>
		/// Announce deploy tokens (if any) and clear them for the hand card id.
		/// </summary>
		public static void AnnounceAndClearOnDeploy( DeploymentCard deployedCard, string handCardId )
		{
			if ( deployedCard == null || string.IsNullOrEmpty( handCardId ) )
				return;

			var tokens = GetTokens( handCardId );
			if ( tokens.Count == 0 )
			{
				ClearTokens( handCardId );
				return;
			}

			string message = FormatDeployAssignment( deployedCard, tokens );
			if ( !string.IsNullOrEmpty( message ) )
				GlowEngine.FindUnityObject<QuickMessage>()?.Show( message );

			ClearTokens( handCardId );
		}
	}
}
