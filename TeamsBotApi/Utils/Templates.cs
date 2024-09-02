using System;
using System.Text.Json;
using TeamsBotApi.Model;

namespace TeamsBotApi.Utils;

public static class Templates
{
	public static Dictionary<string, ConversationTemplate> TemplatesDict { get;}	
	public static List<string> TemplatesList { get;}

	static Templates()
	{
		string[] templates = Directory.GetFiles("../TeamsBotApi/Data/Templates");
		TemplatesDict = new Dictionary<string, ConversationTemplate>();
		TemplatesList = new List<string>();

		foreach (string template in templates)
		{
			string templateData = File.ReadAllText(template);
			ConversationTemplate templateObj = JsonSerializer.Deserialize<ConversationTemplate>(templateData);
			TemplatesDict.Add(templateObj.TemplateName, templateObj);
			TemplatesList.Add(templateObj.TemplateName);
		}
	}
}
