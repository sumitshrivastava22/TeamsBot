using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeamsBotApi.Model;

public class ConversationTemplate
{
	public string? TemplateName { get; set; }

	public List<SectionTemplate>? Sections { get; set; }
}

public class SectionTemplate
{
	public string? SectionName { get; set; }

	public List<QuestionTemplate>? Questions { get; set; }
}

public class QuestionTemplate
{
	public string? QuestionText { get; set; }

	public string? Id { get; set; }

	public string? ResponseType { get; set; }

	public List<string>? Options { get; set; }		
}