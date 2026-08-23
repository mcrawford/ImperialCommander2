using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Saga
{
	public static class AttachmentManager
	{
		/// <summary>
		/// Load attachments from JSON file
		/// </summary>
		public static List<Attachment> LoadAttachments()
		{
			try
			{
				string path = "Languages/" + DataStore.languageCodeList[DataStore.languageCode] + "/attachments";
				var textAsset = Resources.Load<TextAsset>( path );
				if ( textAsset == null )
				{
					Debug.LogError( $"LoadAttachments()::Could not load attachments file at path: {path}" );
					return new List<Attachment>();
				}
				
				string json = textAsset.text;
				Utils.LogWarning( $"LoadAttachments()::Loaded JSON: {json}" );
				
				// Configure JSON settings to handle enum deserialization from strings
				var settings = new JsonSerializerSettings
				{
					Converters = new List<JsonConverter> { new StringEnumConverter() }
				};
				
				var data = JsonConvert.DeserializeObject<AttachmentData>( json, settings );
				if ( data == null )
				{
					Utils.LogError( "LoadAttachments()::Deserialized data is null" );
					return new List<Attachment>();
				}
				
				var attachments = data.attachments ?? new List<Attachment>();
				Utils.LogWarning( $"LoadAttachments()::Loaded {attachments.Count} attachments" );
				
				foreach ( var att in attachments )
				{
					Utils.LogWarning( $"  - Attachment: {att.name} (ID: {att.id})" );
					Utils.LogWarning( att.requiredTraits != null && att.requiredTraits.Length > 0
						? $"    Required traits (any): {string.Join( ", ", att.requiredTraits )}"
						: "    No required traits" );
					Utils.LogWarning( att.excludedTraits != null && att.excludedTraits.Length > 0
						? $"    Excluded traits: {string.Join( ", ", att.excludedTraits )}"
						: "    No excluded traits" );
				}
				
				return attachments;
			}
			catch ( System.Exception e )
			{
				Debug.LogError( $"LoadAttachments()::Error loading attachments: {e.Message}\n{e.StackTrace}" );
				return new List<Attachment>();
			}
		}

		/// <summary>
		/// Check if a group matches required IDs, has any required trait, and no excluded trait.
		/// </summary>
		public static bool MeetsRequirements( DeploymentCard card, Attachment attachment )
		{
			Utils.LogWarning( $"MeetsRequirements()::Checking {card.name} (ID: {card.id}) for attachment {attachment.name}" );
			
			bool hasRequiredGroupID = attachment.requiredGroupIDs == null || attachment.requiredGroupIDs.Length == 0
				|| attachment.requiredGroupIDs.Contains( card.id );

			// A required-traits list is an OR condition: matching any listed trait is sufficient.
			bool hasRequiredTrait = attachment.requiredTraits == null || attachment.requiredTraits.Length == 0
				|| ( card.groupTraits != null && card.groupTraits.Any( t => attachment.requiredTraits.Contains( t ) ) );
			bool hasExcludedTrait = attachment.excludedTraits != null && attachment.excludedTraits.Length > 0
				&& card.groupTraits != null && card.groupTraits.Any( t => attachment.excludedTraits.Contains( t ) );
			bool isEligible = hasRequiredGroupID && hasRequiredTrait && !hasExcludedTrait;
			
			Utils.LogWarning( $"  Has required group ID: {hasRequiredGroupID}, Has required trait: {hasRequiredTrait}, Has excluded trait: {hasExcludedTrait}, Is eligible: {isEligible}" );
			
			return isEligible;
		}

		/// <summary>
		/// Filter groups by attachment requirements
		/// </summary>
		public static List<DeploymentCard> FilterByRequirements( List<DeploymentCard> groups, Attachment attachment )
		{
			Utils.LogWarning( $"FilterByRequirements()::Filtering {groups.Count} groups for attachment {attachment.name}" );
			var eligible = groups.Where( g => MeetsRequirements( g, attachment ) ).ToList();
			Utils.LogWarning( $"FilterByRequirements()::Found {eligible.Count} eligible groups" );
			return eligible;
		}
	}

	public class AttachmentData
	{
		public List<Attachment> attachments;
	}
}
