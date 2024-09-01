using System;
using System.Text.Json;

namespace TeamsBotApi.Model;

public class ConversationTemplate
{
	public string? TemplateName { get; set; }

	public List<QuestionTemplate>? Questions { get; set; }
}

public class QuestionTemplate
{
	public string? QuestionText { get; set; }

	public string? ResponseType { get; set; }

	public List<string>? Options { get; set; }
}