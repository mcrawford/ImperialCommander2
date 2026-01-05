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
					if ( att.excludedTraits != null && att.excludedTraits.Length > 0 )
					{
						Utils.LogWarning( $"    Excluded traits: {string.Join( ", ", att.excludedTraits )}" );
					}
					else
					{
						Utils.LogWarning( "    No excluded traits" );
					}
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
		/// Check if a group meets attachment requirements (doesn't have excluded traits)
		/// </summary>
		public static bool MeetsRequirements( DeploymentCard card, Attachment attachment )
		{
			Utils.LogWarning( $"MeetsRequirements()::Checking {card.name} (ID: {card.id}) for attachment {attachment.name}" );
			
			if ( attachment.excludedTraits == null || attachment.excludedTraits.Length == 0 )
			{
				Utils.LogWarning( $"  No excluded traits - group is eligible" );
				return true;
			}

			if ( card.groupTraits == null || card.groupTraits.Length == 0 )
			{
				Utils.LogWarning( $"  Group has no traits - group is eligible" );
				return true;
			}

			Utils.LogWarning( $"  Group traits: {string.Join( ", ", card.groupTraits )}" );
			Utils.LogWarning( $"  Excluded traits: {string.Join( ", ", attachment.excludedTraits )}" );

			// Group is eligible if it does NOT have any of the excluded traits
			bool hasExcludedTrait = card.groupTraits.Any( t => attachment.excludedTraits.Contains( t ) );
			bool isEligible = !hasExcludedTrait;
			
			Utils.LogWarning( $"  Has excluded trait: {hasExcludedTrait}, Is eligible: {isEligible}" );
			
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

		/// <summary>
		/// Apply an attachment to a deployment group
		/// </summary>
		public static void ApplyAttachment( DeploymentCard card, Attachment attachment, GameObject prefab = null )
		{
			if ( card == null || attachment == null )
			{
				Utils.LogError( "ApplyAttachment()::Card or attachment is null" );
				return;
			}

			// Get or create override for this group
			var ovrd = DataStore.sagaSessionData.gameVars.CreateDeploymentOverride( card.id );

			// Apply modification if present
			if ( !string.IsNullOrEmpty( attachment.modification ) )
			{
				ovrd.modification = attachment.modification;
				ovrd.showMod = true;
				Utils.LogWarning( $"ApplyAttachment()::Applied modification '{attachment.modification}' to {card.name}" );
			}

			// Apply bonus instruction if present
			if ( !string.IsNullOrEmpty( attachment.bonusInstruction ) )
			{
				var instructionText = "{#} " + attachment.name + ": " + attachment.bonusInstruction;
				ovrd.SetInstructionOverride( new ChangeInstructions()
				{
					instructionType = CustomInstructionType.Top,
					theText = instructionText
				} );
				Utils.LogWarning( $"ApplyAttachment()::Applied bonus instruction to {card.name}" );
			}

			// Track the attachment
			DataStore.sagaSessionData.gameVars.activeAttachments[card.id] = attachment.id;

			// Re-initialize the prefab to show the changes if provided
			if ( prefab != null )
			{
				prefab.GetComponent<SagaDGPrefab>().Init( card );
			}

			Utils.LogWarning( $"ApplyAttachment()::Attachment '{attachment.name}' assigned to {card.name}" );
		}
	}

	public class AttachmentData
	{
		public List<Attachment> attachments;
	}
}

