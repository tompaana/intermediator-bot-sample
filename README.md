# Intermediator Bot Sample #

A bot build on [Microsoft Bot Framework](https://dev.botframework.com/) that
routes messages between two users on different channels.

A possible use case for this type of a bot would be a customer service scenario
where the bot relays the messages between a customer and a customer service agent.

See also [Chatbots as Middlemen blog post](http://tomipaananen.azurewebsites.net/?p=1851) related to
this sample.

## Running and testing ##

To test the bot, publish it and connect it to the channels of your choice.
If you are new to bots, please familiarize yourself first with the basics
[here](https://dev.botframework.com/).

I used [Microsoft Bot Framework Emulator](https://docs.botframework.com/en-us/tools/bot-framework-emulator/)
and Slack for testing the bot. To communicate with a remotely hosted bot, you
can use [ngrok](https://ngrok.com/) tunneling software:

1. In emulator open **App Settings**
2. Make sure ngrok path is set
3. Check the localhost port in the emulator log (may require restarting the emulator)
    ![ngrok localhost port in emulator log](Documentation/Screenshots/NgrokLocalhostPortInEmulatorLog.png?raw=true)
4. Launch ngrok: `ngrok http -host-header=rewrite <local host port>`
5. Set the bot end point in emulator (`https://<bot URL>/api/messages`)
6. Set **Microsoft App ID** and **Microsoft App Password**

See also: [Microsoft Bot Framework Emulator wiki](https://github.com/microsoft/botframework-emulator/wiki/Getting-Started)

### The flow ###

| Emulator with ngrok | Slack |
| ------------------- | ----- |
| | ![Initialization](Documentation/Screenshots/Initialization.png?raw=true) |
| ![Request sent](/Documentation/Screenshots/RequestSent.png?raw=true) | ![Request accepted](/Documentation/Screenshots/RequestAccepted.png?raw=true) |
| | ![Direct messaging channel created](/Documentation/Screenshots/DirectMessagingChannelCreated.png?raw=true) |
| ![Conversation in emulator](/Documentation/Screenshots/ConversationInEmulator.png?raw=true) | ![Conversation in Slack](/Documentation/Screenshots/ConversationInSlack.png?raw=true) |


## Implementation ##

### Terminology ###

| Term | Description |
| ---- | ----------- |
| Aggregation (channel) | A channel where the chat requests are sent. The users in the aggregation channel can accept the requests. |
| Engagement | Is created when a request is accepted - the acceptor and the one accepted form an engagement (1:1 chat where the bot relays the messages between the users). |
| Party | A user/bot in a specific conversation. |
| Conversation client | A reqular user e.g. a customer. |
| Conversation owner | E.g. a customer service **agent**. |

### Interfaces and classes ###

**[Party](/IntermediatorBotSample/MessageRouting/Party.cs)** holds the details
of specific user/bot in a specific conversation. Note that the bot collects
parties from all the conversations it's in and there will be a `Party` instance
of a user/bot for each conversation (i.e. there can be multiple parties for a
single user/bot). One can think of `Party` as a full address the bot needs in
order to send a message to the user in a conversation. The `Party` instances are
stored in routing data.

**[IRoutingDataManager](/IntermediatorBotSample/MessageRouting/IRoutingDataManager.cs)**
manages the parties (users/bot), aggregation channel details, the list of
engaged parties and pending requests. **Note** that this data should be stored
in e.g. a blob storage! For testing it is OK to have the data in memory.

**[MessageRouterManager](/IntermediatorBotSample/MessageRouting/MessageRouterManager.cs)**
is the main class of the sample. It manages the routing data and handles
commands to the bot and executes the actual message mediation between the
parties engaged in a conversation. The most important methods in this class
are as follows:

* `AddParty`: Adds a new party to the routing data. It is recommended to use `MakeSurePartiesAreTracked` instead of this for adding parties.
* `RemoveParty`: Removes all the instances related to the given party from the routing data (since there can be multiple - one for each conversation).
* `MakeSurePartiesAreTracked`: A convenient method for adding parties. The given parties are added if they are new.
* `IntiateEngagement`: Creates and posts a new chat request.
* `AddEngagementAsync`: Establishes an engagement between the given parties. This method is called when a chat request is accepted.
* `HandleMessageAsync`: Handles the incoming messages: Relays the messages between engaged parties.

### Taking the classes into use ###

The most convenient place to use the aforementioned classes is in the
**[MessagesController](/IntermediatorBotSample/Controllers/MessagesController.cs)**
class - you can first call the methods in `MessageRouterManager` and, for
instance, if no action is taken by the manager, you can forward the `Activity`
to a `Dialog`:

```cs
public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
{
    if (activity.Type == ActivityTypes.Message)
    {
        // Get the message router manager instance and let it handle the activity
        MessageRouterResult result = await MessageRouterManager.Instance.HandleActivityAsync(activity, true);

        if (result.Type == MessageRouterResultType.NoActionTaken)
        {
            // No action was taken
            // You can forward the activity to e.g. a dialog here
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


## Acknowledgements ##

Although you can't see it in the change history,
[Edouard Mathon](https://github.com/edouard-mathon) added
multi-aggregation-channel support (many thanks!), which in human talk means that
the chat requests can be received by multiple channels/conversations.
