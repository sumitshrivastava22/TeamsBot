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

	private static async Task SendResponse(ITurnContext<IMessageActivity> turnContext, string userMessage, ConversationData conversationData, CancellationToken cancellationToken)
	{
		var message = CreateMessage("Please select the Template", Templates.TemplatesList);
		if (conversationData.conversationTemplate != null)
		{
			QuestionResponse response = new QuestionResponse {Id = conversationData.conversationTemplate.Questions[conversationData.ActiveQuestion].Id, Response= userMessage};
			conversationData.conversationResponse.QuestionResponses.Add(response);
			ConversationTemplate conversationTemplate = conversationData.conversationTemplate;
			if (conversationData.ActiveQuestion + 1 < conversationTemplate.Questions.Count)
			{
				var question = conversationTemplate.Questions[conversationData.ActiveQuestion + 1];
				message = CreateMessage(question.QuestionText, question.Options);
				conversationData.ActiveQuestion = conversationData.ActiveQuestion + 1;
			}
			else
			{
				message = CreateMessage($"Conversation completed for template {conversationData.conversationTemplate.TemplateName}");
				string responseData = JsonSerializer.Serialize<ConversationResponse>(conversationData.conversationResponse);
				File.WriteAllText($"../TeamsBotApi/Responses/{conversationData.conversationTemplate.TemplateName}.json", responseData);
				conversationData.conversationTemplate = null;
				conversationData.ActiveQuestion = 0;				
			}
		}
		else if (Templates.TemplatesList.Contains(userMessage))
		{
			ConversationTemplate conversationTemplate = Templates.TemplatesDict[userMessage];
			conversationData.conversationTemplate = conversationTemplate;
			conversationData.conversationResponse = new ConversationResponse{TemplateName = userMessage, QuestionResponses = new List<QuestionResponse>()};
			conversationData.ActiveQuestion = 0;
			var question = conversationTemplate.Questions[0];
			message = CreateMessage(question.QuestionText, question.Options);
		}

		await turnContext.SendActivityAsync(message, cancellationToken);

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
