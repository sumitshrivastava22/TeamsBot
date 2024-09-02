using System;

namespace TeamsBotApi.Model;

public class ConversationData
{
	public int ActiveQuestion { get; set; }

	public ConversationTemplate? conversationTemplate{ get; set; }

	public ConversationResponse conversationResponse { get; set; }
}
