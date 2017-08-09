# Bot-HandOff for C# Microsoft Bot Builder SDK

This package is based off the github repo here: https://github.com/tompaana/intermediator-bot-sample - please raise any bugs/issues or feature requests [here](https://github.com/tompaana/intermediator-bot-sample/issues).

## Using the Bot-HandOff nuget package

Once you've added a reference to this package you will need to modify your MessageController.cs in order for it to listen and process messages.  

### MessageController.cs

```
public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
{
	if (activity.Type == ActivityTypes.Message)
	{
		// Get the message router manager instance and let it handle the activity
		MessageRouterResult result = await MessageRouterManager.Instance.HandleActivityAsync(activity, false);

		if (result.Type == MessageRouterResultType.NoActionTaken)
		{
			await Conversation.SendAsync(activity, () => new RootDialog());
		}
	}
	else
	{
		HandleSystemMessage(activity);
	}
	var response = Request.CreateResponse(HttpStatusCode.OK);
	return response;
}
```

Here you can also decide any trigger words to invoke a request for human assistance eg:

```
public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
{
	if (activity.Type == ActivityTypes.Message)
	{
		// Get the message router manager instance and let it handle the activity
		MessageRouterResult result = await MessageRouterManager.Instance.HandleActivityAsync(activity, false);

		if (result.Type == MessageRouterResultType.NoActionTaken)
		{
			// Look out for keywords for assistance
			if (!string.IsNullOrEmpty(activity.Text) && activity.Text.ToLower().Contains("human"))
			{
				await MessageRouterManager.Instance.InitiateEngagementAsync(activity);
			}
			else
			{
				await Conversation.SendAsync(activity, () => new RootDialog());
			}
		}
	}
	else
	{
		HandleSystemMessage(activity);
	}
	var response = Request.CreateResponse(HttpStatusCode.OK);
	return response;
}
```

You'll also want to ensure you handle system messages correctly - to ensure that conversations between agents/users aren't left in an unknown state eg:

```
private async Task<Activity> HandleSystemMessage(Activity message)
{
    MessageRouterManager messageRouterManager = MessageRouterManager.Instance;

    if (message.Type == ActivityTypes.DeleteUserData)
    {
        // If we handle user deletion, return a real message
        Party senderParty = MessagingUtils.CreateSenderParty(message);

        if (await messageRouterManager.RemovePartyAsync(senderParty))
        {
            return message.CreateReply($"Data of user {senderParty.ChannelAccount?.Name} removed");
        }
    }
    else if (message.Type == ActivityTypes.ConversationUpdate)
    {
        // Handle conversation state changes, like members being added and removed
        // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
        // Not available in all channels
        if (message.MembersRemoved != null && message.MembersRemoved.Count > 0)
        {
            foreach (ChannelAccount channelAccount in message.MembersRemoved)
            {
                Party party = new Party(
                    message.ServiceUrl, message.ChannelId, channelAccount, message.Conversation);

                if (await messageRouterManager.RemovePartyAsync(party))
                {
                    System.Diagnostics.Debug.WriteLine($"Party {party.ToString()} removed");
                }
            }
        }
    }
    else if (message.Type == ActivityTypes.ContactRelationUpdate)
    {
        // Handle add/remove from contact lists
        // Activity.From + Activity.Action represent what happened
    }
    else if (message.Type == ActivityTypes.Typing)
    {
        // Handle knowing that the user is typing
    }
    else if (message.Type == ActivityTypes.Ping)
    {
    }

    return null;
}
```
## Scenario 2: Channel <-> call center (agent UI)

For the scenario where you want to have a single call agent managing multiple conversations you can use the additional Agent UI described in the github repo.  You will also need to add an additional APIController to your bot for the Agent UI to call back on.

Add an AgentController.cs file to your bot eg:
```
public class AgentController : ApiController
{
    private const string ResponseNone = "None";

    /// <summary>
    /// Handles requests sent by the Agent UI.
    /// If there are no aggregation channels set and one or more pending requests exist,
    /// the oldest request is processed and sent to the Agent UI.
    /// </summary>
    /// <param name="id">Not used.</param>
    /// <returns>The details of the user who made the request or "None", if no pending requests
    /// or if one or more aggregation channels are set up.</returns>
    [EnableCors("*", "*", "*")]
    public string GetAgentById(int id)
    {
        string response = ResponseNone;
        MessageRouterManager messageRouterManager = MessageRouterManager.Instance;
        IRoutingDataManager routingDataManager = messageRouterManager.RoutingDataManager;

        if (routingDataManager.GetAggregationParties().Count == 0
            && routingDataManager.GetPendingRequests().Count > 0)
        {
            try
            {
                Party conversationClientParty = messageRouterManager.RoutingDataManager.GetPendingRequests().First();
                messageRouterManager.RoutingDataManager.RemovePendingRequest(conversationClientParty);
                response = conversationClientParty.ToIdString();
            }
            catch (InvalidOperationException e)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to handle a pending request: {e.Message}");
            }
        }

        return response;
    }
}
```

## Supervising conversations

Once the above has been added you can run your bot and use the following special commands to begin supervising any end user conversation to the bot or allow any user to request human assistance.

### Supervisor/Agent commands

These are discreet commands that the bot will listen out for and action on behalf of the agent:

| Command    |    Description            |
|------------|---------------------------|
| Watch		 | This will allow the agent to start listening for any requests for human assistance |
| Connect	 | When an end-user has requested assistance, the agent will automatically be prompted to Accept/Reject the conversation - connecting accepts the request and allows the agent to take over the conversation on behalf of the bot |
| Disconnect | This will "hang-up" the agent from the conversation and leave the end user conversing directly with the bot |
| List       | This will show any requests for human assistance that have been requested by all users |
| Reset      | This clears the in-memory state of all active conversation management for bot handoffs


