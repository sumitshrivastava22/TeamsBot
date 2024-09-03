using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using TeamsBotApi.Model;
using TeamsBotApi.Utils;

namespace TeamsBotApi.Bots;

public class TeamsBot : ActivityHandler
{
	private readonly ConversationState _conversationState;
	private readonly IStatePropertyAccessor<ConversationData> _conversationDataAccessor;


	public TeamsBot(ConversationState conversationState)
	{
		_conversationState = conversationState;
		_conversationDataAccessor = conversationState.CreateProperty<ConversationData>("ConversationData");
	}

	protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
	{
		var conversationData = await _conversationDataAccessor.GetAsync(turnContext, () => new ConversationData(), cancellationToken);

		var userMessage = turnContext.Activity.Text;
		//await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
		await SendResponse(turnContext, userMessage, conversationData, cancellationToken);

		await _conversationDataAccessor.SetAsync(turnContext, conversationData, cancellationToken);
		await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
	}

	private static async Task SendResponse(
	ITurnContext<IMessageActivity> turnContext,
	string userMessage,
	ConversationData conversationData,
	CancellationToken cancellationToken)
	{
		IMessageActivity message = null;

		if (conversationData.conversationTemplate != null || Templates.TemplatesList.Contains(userMessage))
			message = RunTemplateConversation(turnContext, userMessage, conversationData, cancellationToken);
		else
		{
			switch(userMessage)
			{
				case "Create Excel":
					message = CreateMessage("Functionality to be implemented");
					break;

				case "Go To Dashboard":
					message = CreateMessage("Functionality to be implemented");
					break;

				case "Add new survey Data":
					message = CreateMessage("Please Select Template", Templates.TemplatesList);
					break;

				default:
					message = CreateMessage("Welcome Card", "Please Select an Option", ["Add new survey Data", "Create Excel", "Go To Dashboard"]);
					break;
			}			
		}

		await turnContext.SendActivityAsync(message, cancellationToken);	
	}

	private static IMessageActivity RunTemplateConversation(
	ITurnContext<IMessageActivity> turnContext,
	string userMessage,
	ConversationData conversationData,
	CancellationToken cancellationToken)
	{
		IMessageActivity message;

		// If there's an active conversation template
		if (conversationData.conversationTemplate != null)
		{
			HandleActiveConversation(userMessage, conversationData);
			message = GetNextMessage(conversationData);
		}
		else if (Templates.TemplatesList.Contains(userMessage)) // If user selects a valid template
		{
			message = InitializeConversation(userMessage, conversationData);			
		}
		else
		{
			message = CreateMessage("Please select the Template", Templates.TemplatesList);
		}

		return message;
	}

	private static IMessageActivity InitializeConversation(string userMessage, ConversationData conversationData)
	{
		var conversationTemplate = Templates.TemplatesDict[userMessage];
		conversationData.conversationTemplate = conversationTemplate;
		conversationData.conversationResponse = new ConversationResponse
		{
			TemplateName = userMessage,
			SectionResponses = new List<SectionResponse>()
		};
		return InitializeSection(userMessage, conversationData);
	}

	private static IMessageActivity InitializeSection(string userMessage, ConversationData conversationData)
	{
		conversationData.ActiveSection = 0;
		conversationData.sectionTemplate = conversationData.conversationTemplate.Sections[0];

		conversationData.sectionResponse = new SectionResponse
		{
			SectionName = conversationData.sectionTemplate.SectionName,
			QuestionResponses = new List<QuestionResponse>()
		};

		conversationData.ActiveQuestion = 0;		
		var nextQuestion = conversationData.sectionTemplate.Questions[conversationData.ActiveQuestion];
		return CreateMessage(nextQuestion.QuestionText, conversationData.conversationTemplate.Sections[conversationData.ActiveSection].SectionName, nextQuestion.Options);
	}

	private static void HandleActiveConversation(string userMessage, ConversationData conversationData)
	{
		// Record the user's response
		var response = new QuestionResponse
		{
			Id = conversationData.sectionTemplate.Questions[conversationData.ActiveQuestion].Id,
			Response = userMessage
		};
		conversationData.sectionResponse.QuestionResponses.Add(response);
	}

	private static IMessageActivity GetNextMessage(ConversationData conversationData)
	{
		var sectionTemplate = conversationData.sectionTemplate;

		// Check if there are more questions
		if (conversationData.ActiveQuestion + 1 < sectionTemplate.Questions.Count)
		{
			var nextQuestion = sectionTemplate.Questions[++conversationData.ActiveQuestion];
			return CreateMessage(nextQuestion.QuestionText, conversationData.conversationTemplate.Sections[conversationData.ActiveSection].SectionName, nextQuestion.Options);
		}

		// No more questions, finalize the conversation
		return FinalizeSection(conversationData);		
	}

	private static IMessageActivity FinalizeSection(ConversationData conversationData)
	{
		conversationData.conversationResponse.SectionResponses.Add(conversationData.sectionResponse);
		conversationData.sectionResponse = new SectionResponse
		{
			SectionName = conversationData.sectionTemplate.SectionName,
			QuestionResponses = new List<QuestionResponse>()
		};		

		if(conversationData.ActiveSection +1 < conversationData.conversationTemplate.Sections.Count)
		{
			conversationData.sectionTemplate = conversationData.conversationTemplate.Sections[++conversationData.ActiveSection];						
			conversationData.ActiveQuestion = 0;		
			var nextQuestion = conversationData.sectionTemplate.Questions[conversationData.ActiveQuestion];
			return CreateMessage(nextQuestion.QuestionText, conversationData.conversationTemplate.Sections[conversationData.ActiveSection].SectionName, nextQuestion.Options);
		}
		else
		{
			return FinalizeConversation(conversationData);			
		}
	}	

	private static IMessageActivity FinalizeConversation(ConversationData conversationData)
	{
		var responseData = JsonSerializer.Serialize(conversationData.conversationResponse);
		var filePath = $"../TeamsBotApi/Responses/{conversationData.conversationTemplate.TemplateName}.json";
		File.WriteAllText(filePath, responseData);

		string templateName = conversationData.conversationTemplate.TemplateName;
		// Reset conversation data
		conversationData.conversationTemplate = null;
		conversationData.ActiveQuestion = 0;
		return CreateMessage($"Conversation completed for template {templateName}");
	}	

	private static IMessageActivity CreateMessage(string title, string subtitle, string text, List<string>? options = null)
	{
		if (options == null || options.Count == 0)
		{
			var heroCard = new HeroCard
			{
				Title = title,
				Subtitle = subtitle,				
				Text = text
			};
			return MessageFactory.Attachment(heroCard.ToAttachment());
		}
		else
		{
			List<CardAction> actionsList = new List<CardAction>();
			foreach (string option in options)
			{
				var action = new CardAction { Type = ActionTypes.MessageBack, Text = option, Value = option, DisplayText = option, Title = option };
				actionsList.Add(action);
			}

			var heroCard = new HeroCard
			{
				Title = title,
				Subtitle = subtitle,
				Buttons = actionsList
			};

			return MessageFactory.Attachment(heroCard.ToAttachment());
		}
	}

	private static IMessageActivity CreateMessage(string title, List<string>? options = null)
	{
		return CreateMessage(title, null, null, options);
	}

	private static IMessageActivity CreateMessage(string title, string subtitle, List<string>? options = null)
	{
		return CreateMessage(title, subtitle, null, options);
	}
}
