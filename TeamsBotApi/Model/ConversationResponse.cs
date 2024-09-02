using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeamsBotApi.Model;

public class ConversationResponse
{
	public string? TemplateName { get; set; }

	public List<SectionResponse>? SectionResponses { get; set; }
}

public class SectionResponse
{
	public string? SectionName { get; set; }

	public List<QuestionResponse>? QuestionResponses { get; set; }
}

public class QuestionResponse
{
	public string? Id { get; set; }

	public string? Response { get; set; }

}