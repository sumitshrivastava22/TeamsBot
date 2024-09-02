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

		await turnContext.SendActivityAsync(message, cancellationToken);
	}

	private static void HandleActiveConversation(string userMessage, ConversationData conversationData)
	{
		// Record the user's response
		var response = new QuestionResponse
		{
			Id = conversationData.conversationTemplate.Questions[conversationData.ActiveQuestion].Id,
			Response = userMessage
		};
		conversationData.conversationResponse.QuestionResponses.Add(response);
	}

	private static IMessageActivity GetNextMessage(ConversationData conversationData)
	{
		var conversationTemplate = conversationData.conversationTemplate;

		// Check if there are more questions
		if (conversationData.ActiveQuestion + 1 < conversationTemplate.Questions.Count)
		{
			var nextQuestion = conversationTemplate.Questions[++conversationData.ActiveQuestion];
			return CreateMessage(nextQuestion.QuestionText, nextQuestion.Options);
		}

		// No more questions, finalize the conversation
		FinalizeConversation(conversationData);
		return CreateMessage($"Conversation completed for template {conversationTemplate.TemplateName}");
	}

	private static void FinalizeConversation(ConversationData conversationData)
	{
		var responseData = JsonSerializer.Serialize(conversationData.conversationResponse);
		var filePath = $"../TeamsBotApi/Responses/{conversationData.conversationTemplate.TemplateName}.json";
		File.WriteAllText(filePath, responseData);

		// Reset conversation data
		conversationData.conversationTemplate = null;
		conversationData.ActiveQuestion = 0;
	}

	private static IMessageActivity InitializeConversation(string userMessage, ConversationData conversationData)
	{
		var conversationTemplate = Templates.TemplatesDict[userMessage];
		conversationData.conversationTemplate = conversationTemplate;
		conversationData.conversationResponse = new ConversationResponse
		{
			TemplateName = userMessage,
			QuestionResponses = new List<QuestionResponse>()
		};
		conversationData.ActiveQuestion = 0;		
		var nextQuestion = conversationTemplate.Questions[conversationData.ActiveQuestion];
		return CreateMessage(nextQuestion.QuestionText, nextQuestion.Options);
	}

	private static IMessageActivity CreateMessage(string title, List<string>? options = null)
	{
		if (options == null || options.Count == 0)
		{
			return MessageFactory.Text(title, title);
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
				Buttons = actionsList
			};

			return MessageFactory.Attachment(heroCard.ToAttachment());
		}
	}
}
