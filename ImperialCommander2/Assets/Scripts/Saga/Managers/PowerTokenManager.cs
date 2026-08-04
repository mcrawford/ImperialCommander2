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
