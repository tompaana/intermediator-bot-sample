# Intermediator Bot Sample #

A bot build on [Microsoft Bot Framework](https://dev.botframework.com/) that
routes messages between two users on different channels.

A possible use case for this type of a bot would be a customer service scenario
where the bot relays the messages between a customer and a customer service agent.

See also [Chatbots as Middlemen blog post](http://tomipaananen.azurewebsites.net/?p=1851) related to
this sample.

## Getting started ##

To test the bot, publish it in
[Microsoft Bot Framework portal](https://dev.botframework.com) and connect it to
the channels of your choice. If you are new to bots, please familiarize yourself
first with the basics [here](https://dev.botframework.com/).
[Microsoft Bot Framework Emulator](https://docs.botframework.com/en-us/tools/bot-framework-emulator/)
is a great tool for testing and debugging. To communicate with a remotely hosted
bot, you should use [ngrok](https://ngrok.com/) tunneling software:

1. In emulator open **App Settings**
2. Make sure ngrok path is set
3. Check the localhost port in the emulator log (may require restarting the emulator)
    ![ngrok localhost port in emulator log](Documentation/Screenshots/NgrokLocalhostPortInEmulatorLog.png?raw=true)
4. Launch ngrok: `ngrok http -host-header=rewrite <local host port>`
5. Set the bot end point in emulator (`https://<bot URL>/api/messages`)
6. Set **Microsoft App ID** and **Microsoft App Password**

See also: [Microsoft Bot Framework Emulator wiki](https://github.com/microsoft/botframework-emulator/wiki/Getting-Started)

### Scenario 1: Channel <-> channel ###

This scenario utilizes an aggregation concept (see the terminology table in this
document). One or more channels act as aggregated channels where the customer
requests (for human assistance) are sent. The conversation owners (e.g. customer
service agents) then accept or reject the requests.

Once you have published the bot, go to the channel you want to receive the
requests and issue the following command to the bot (given that you haven't
changed the default bot command handler or the command itself):

```
@BOT_NAME add aggregation
```
    
In case mentions are not supported, you can also use the command keyword:

```
command add aggregation
```
    
Now all the requests from another channels are forwarded to this channel.

<!--

## The flow ##

| Emulator with ngrok | Slack |
| ------------------- | ----- |
| | ![Initialization](Documentation/Screenshots/Initialization.png?raw=true) |
| ![Request sent](/Documentation/Screenshots/RequestSent.png?raw=true) | ![Request accepted](/Documentation/Screenshots/RequestAccepted.png?raw=true) |
| | ![Direct messaging channel created](/Documentation/Screenshots/DirectMessagingChannelCreated.png?raw=true) |
| ![Conversation in emulator](/Documentation/Screenshots/ConversationInEmulator.png?raw=true) | ![Conversation in Slack](/Documentation/Screenshots/ConversationInSlack.png?raw=true) |

-->

### Scenario 2: Channel <-> call center (agent UI) ###

In this scenario the conversation owners (e.g. customer service agents) access
the bot via the webchat component, [Agent UI](https://github.com/billba/agent),
implemented by [Bill Barnes](https://github.com/billba). Each customer request
(for human assistance) automatically opens a new chat window in the agent UI.
To set this up, follow these steps:

0. Make sure you have [Node.js](https://nodejs.org) installed
1. Clone or download [the Agent UI repository](https://github.com/billba/agent)
2. Inside `index.ts`, update the line below with your bot's endpoint:

    `fetch("https://YOUR_BOT_ENDPOINT/api/agent/1")`
    
    Example: `fetch("https://intermediatorbotsample.azurewebsites.net/api/agent/1")`

3. Inside `index.ts`, update the line below with your bot secret key

    ```js
    iframe.src = 'botchat?s=YOUR_WEBCHAT_SECRET_ID';
    ```
    
    * The bot secret key can be found in your bot's profile in
      [the portal](https://dev.botframework.com/bots)
    * Click on the **Edit** button next to the **Web Chat** channel to locate
      the secret key
    
    ![Edit button in the portal](Documentation/Screenshots/RetrievingBotSecret1.png?raw=true)
    
    * If your **Configure Web Chat** page is blank, create a new site by
      clicking **Add new site** and two bot secret keys will be generated for you:
    
    ![Bot secret on Configure Web Chat page](Documentation/Screenshots/RetrievingBotSecret2.png?raw=true)

4. Run `npm install` to get the npm packages 

    * You only need to run this command once, unless you add other node packages
      to the project

5. Run `npm run build` to build the app 

    * You need to run this every time you make changes to the code before you
      start the application

6. Run `npm run start` to start the app
7. Go to `http://localhost:8080` to see the Agent UI


## Implementation ##

### Terminology ###

| Term | Description |
| ---- | ----------- |
| Aggregation (channel) | A channel where the chat requests are sent. The users in the aggregation channel can accept the requests. **If you went with the agent UI approach, aggregation concept is not used.** |
| Engagement | Is created when a request is accepted - the acceptor and the one accepted form an engagement (1:1 chat where the bot relays the messages between the users). |
| Party | A user/bot in a specific conversation. |
| Conversation client | A reqular user e.g. a customer. |
| Conversation owner | E.g. a customer service **agent**. |

### Interfaces ###

#### [IRoutingDataManager](/IntermediatorBotSample/MessageRouting/IRoutingDataManager.cs) ####

An interface for managing the parties (users/bot), aggregation channel details, the list of
engaged parties and pending requests. **Note:** In production this data should be stored
in e.g. a table storage!
[LocalRoutingDataManager](/IntermediatorBotSample/MessageRouting/LocalRoutingDataManager.cs) is
provided for testing, but it provides only an in-memory solution.

#### [IMessageRouterResultHandler](/IntermediatorBotSample/MessageRouting/IMessageRouterResultHandler.cs) ####

An interface for handling message router operation results. You can consider
the result handler as an event handler, but since asynchronicity (that comes
with an actual event handler in C#) may cause all kind of problems with the bot
framework *(namely that the execution of the code that makes the bot send
messages related to an incoming activity should not continue once
`MessagesController.Post` is exited)*, it is more convenient to handle the
results this way. The implementation of this interface defines the bot
responses for specific events (results) so it is a natural place to have
localization in (should your bot application require it).

A default result handler implementation is provided by
[DefaultMessageRouterResultHandler](/IntermediatorBotSample/MessageRouting/DefaultMessageRouterResultHandler.cs)
class, but you can easily replace this with your own
(see `MessageRouterMananger` class documentation below).

#### [IBotCommandHandler](/IntermediatorBotSample/MessageRouting/IBotCommandHandler.cs) ####

An interface for handling commands to the bot. This project cares little what
bot commands (if any) are defined and what they should do, although a default
implementation
([DefaultBotCommandHandler](/IntermediatorBotSample/MessageRouting/DefaultBotCommandHandler.cs))
has been provided. Like with the result handler, you can implement and set your
own command handler to the `MessageRouterManager` class instance.

### MessageRouterManager class ###

**[MessageRouterManager](/IntermediatorBotSample/MessageRouting/MessageRouterManager.cs)**
is the main class of the project. It manages the routing data (using the
provided `IRoutingDataManager` implementation) and handles the commands to
the bot (`IBotCommandHandler`) and executes the actual message mediation
between the parties engaged in a conversation.

#### Properties ####

* `Instance` is a static property providing the singleton instance of the class.
* `RoutingDataManager`: The implementation of `IRoutingDataManager` interface
  in use. In case you want to replace the default implementation with your own,
  set it in `App_Start\WebApiConfig.cs`.
* `ResultHandler`: The implementation of `IMessageRouterResultHandler` interface
  in use. In case you want to replace the default implementation with your own,
  set it in `App_Start\WebApiConfig.cs`.
* `CommandHandler`: The implementation of `IBotCommandHandler` interface
  in use. In case you want to replace the default implementation with your own,
  set it in `App_Start\WebApiConfig.cs`.

#### Methods ####

* **`HandleActivityAsync`**: In simple cases this is the only method you need
  to call in your `MessagesController` class. It will track the users 
  (stores their information), handle the commands, forward messages between
  users engaged in a conversation and handle the results (by sending them to
  the provided result handler) automatically. The return value
  (`MessageRouterResult`) will indicate whether the message routing logic
  consumed the activity or not. If the activity was ignored by the message
  routing logic, you can e.g. forward it to your dialog.
* `SendMessageToPartyByBotAsync`: Utility method to make the bot send a given
  message to a given user.
* `BroadcastMessageToAggregationChannels`: Sends the given message to all the
  aggregation channels.
* `MakeSurePartiesAreTracked`: A convenient method for adding parties.
  The given parties are added if they are new. This method is called by
  `HandleActivityAsync` so you don't need to bother calling this explicitly
  yourself.
* `RemovePartyAsync`: Removes all the instances related to the given party from
  the routing data (since there can be multiple - one for each conversation).
  Will also remove any pending requests of the party in question as well as end
  all conversations of this specific user.
* `IntiateEngagementAsync`: Creates a request on behalf of the sender of the
  activity. The result handler forward the request to the owners
  (e.g. customer service agents).
* `RejectPendingRequestAsync`: Removes the pending request of the given user.
  The result handler implementation should notify the user, if necessary.
* `AddEngagementAsync`: Establishes an engagement between the given parties.
  This method should be called when a chat request is accepted.
* `HandleBackChannelMessageAsync`: Handles (hidden) messages from the agent UI
  component.
* `HandleMessageAsync`: Handles the incoming messages: Relays the messages
  between engaged parties.

### Other classes ###

**[Party](/IntermediatorBotSample/MessageRouting/Party.cs)** holds the details
of specific user/bot in a specific conversation. Note that the bot collects
parties from all the conversations it's in and there will be a `Party` instance
of a user/bot for each conversation (i.e. there can be multiple parties for a
single user/bot). One can think of `Party` as a full address the bot needs in
order to send a message to the user in a conversation. The `Party` instances are
stored in routing data.

**[MessageRouterResult](/IntermediatorBotSample/MessageRouting/MessageRouterResult.cs)**
is the return value for more complex operations of the `MessageRouterManager`
class not unlike custom `EventArgs` implementations, but due to the problems
that using actual event handlers can cause, these return values are handled
by a dedicated `IMessageRouterResultHandler` implementation.

**[AgentController](/IntermediatorBotSample/Controllers/AgentController.cs)**
provides the pending requests from customers to the agent UI webchat component.

### Taking the code into use ###

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
        MessageRouterResult result = await MessageRouterManager.Instance.HandleActivityAsync(activity, false);

        if (result.Type == MessageRouterResultType.NoActionTaken)
        {
            // No action was taken by the message router manager. This means that the user
            // is not engaged in a 1:1 conversation with a human (e.g. customer service
            // agent) yet.
            //
            // You can, for example, check if the user (customer) needs human assistance
            // here or forward the activity to a dialog. You could also do the check in
            // the dialog too...
            //
            // Here's an example:
            if (!string.IsNullOrEmpty(activity.Text) && activity.Text.ToLower().Contains("human"))
            {
                await MessageRouterManager.Instance.InitiateEngagement(activity);
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


## Acknowledgements ##

* [Lilian Kasem](https://github.com/liliankasem) implemented and documented the
  agent UI support. She also had a significant role in the making of this
  sample.
* Although you can't see it in the change history,
  [Edouard Mathon](https://github.com/edouard-mathon) added
  multi-aggregation-channel support (many thanks!), which in human talk means
  that the chat requests can be received by multiple channels/conversations.
