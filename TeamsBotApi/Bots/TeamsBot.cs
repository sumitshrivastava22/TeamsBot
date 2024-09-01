using System.Text;
using System.Text.Json;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using TeamsBotApi.Model;

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
        string templatePath = "./Data/Templates/PersonalTemplate.json";
        string templateContent = File.ReadAllText(templatePath);
        ConversationTemplate conversationTemplate = JsonSerializer.Deserialize<ConversationTemplate>(templateContent);

        string response = "Please select the Template";
        if (!string.IsNullOrEmpty(conversationData.ActiveTemplate))
        {
            if (conversationData.ActiveQuestion + 1 < conversationTemplate.Questions.Count)
            {
                response = conversationTemplate.Questions[conversationData.ActiveQuestion + 1].QuestionText;
                conversationData.ActiveQuestion = conversationData.ActiveQuestion + 1;
            }
            else
            {
                response = $"Conversation completed for template {conversationData.ActiveTemplate}";
                conversationData.ActiveTemplate = "";
                conversationData.ActiveQuestion = 0;
            }
        }
        else if (userMessage == conversationTemplate.TemplateName)
        {
            conversationData.ActiveTemplate = conversationTemplate.TemplateName;
            conversationData.ActiveQuestion = 0;
            response = conversationTemplate.Questions[0].QuestionText;
        }
        await turnContext.SendActivityAsync(MessageFactory.Text(response, response), cancellationToken);

    }
}
