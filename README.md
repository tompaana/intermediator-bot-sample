# Intermediator Bot Sample #

[![Build status](https://ci.appveyor.com/api/projects/status/i1u8puahyxl79ha6?svg=true)](https://ci.appveyor.com/project/tompaana/intermediator-bot-sample)

This is a sample bot, built with the [Microsoft Bot Framework](https://dev.botframework.com/) (v4),
that routes messages between two users on different channels. This sample utilizes the
[Bot Message Routing (component) project](https://github.com/tompaana/bot-message-routing).
The general gist of the message routing is explained in this article:
[Chatbots as Middlemen blog post](https://tompaana.github.io/content/chatbots_as_middlemen.html).

A possible use case for this type of a bot would be a customer service scenario where the bot relays
the messages between a customer and a customer service agent.

This is a C# sample targeting the latest version (v4) of the Microsoft Bot Framework. The sample
did previously target the v3.x and you can find that last release
[here](https://github.com/tompaana/intermediator-bot-sample/releases/tag/v1.1).

If you prefer **Node.js**, fear not, there are these two great samples to look into:

* [botframework-v4-handoff](https://github.com/GeekTrainer/botframework-v4-handoff)
* [Bot-HandOff (v3)](https://github.com/palindromed/Bot-HandOff)

#### Contents ####

* [Getting started](#getting-started)
* [Deploying the bot](#deploying-the-bot)
* [App settings and credentials](#app-settings-and-credentials)
* [Testing the hand-off](#testing-the-hand-off)
* [About the implementation](#implementation)
* [Custom agent portal](#what-if-i-want-to-have-a-custom-agent-portalchannel)
* [Helpful links](#see-also)

## Getting started ##

Since this is an advanced bot scenario, the prerequisites include that you are familiar with the
basic concepts of the Microsoft Bot Framework and you know the C# programming language. Before
getting started it is recommended that you have the following tools installed:

* [Visual Studio IDE](https://www.visualstudio.com/vs/)
* [ngrok](https://ngrok.com/)
* [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator) ([download](https://github.com/Microsoft/BotFramework-Emulator/releases))
    * [Debug bots with the Bot Framework Emulator](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-debug-emulator?view=azure-bot-service-4.0)

Altough the bot can be practically hosted anywhere, the deployment instructions (below) are for
Azure. If you don't have an Azure subscription yet, you can get one for free here:
[Create your Azure free account today](https://azure.microsoft.com/en-us/free/).

## Deploying the bot ##

This sample demonstrates routing messages between different users on different channels. Hence,
using only the emulator to test the sample may prove difficult. To utilize other channels, you must
first compile and publish the bot:

1. Open the solution (`IntermediatorBotSample.sln`) in Visual Studio/your IDE and make sure it
   compiles without any errors (or warnings)
2. Follow the steps in this article carefully:
   [Deploy your bot to Azure](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-deploy-azure?view=azure-bot-service-4.0)
   * Top tip: Create a new [Azure resource group](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-overview#resource-groups)
     for the app so that if stuff goes wrong, it's really easy to just delete the whole group and
     start over
   * Having issues testing the bot (as in "The dang thing doesn't work!!") - check the following:
     * Did you remember to include `/api/messages` in the messaging endpoint
       (Bot Channels Registration/Settings)?
     * Did you remember to create and add the credentials (`MicrosoftAppId` and `MicrosoftAppPassword`)?
3. Add the credentials (`MicrosoftAppId` and `MicrosoftAppPassword`) to the
   [`appsettings.json` file](/IntermediatorBotSample/appsettings.json) and republish the bot - now
   all you need to do to republish is to right-click the app project in the **Solution Explorer** in
   Visual Studio, select **Publish...** and click the **Publish** button on the tab (named in the
   sample "IntermediatorBotSample").

## App settings and credentials ##

App settings and credentials are available in the
[`appsettings.json`](/IntermediatorBotSample/appsettings.json)
file of this sample. The file contains both bot and storage credentials as well as the settings that
can be used to tailor the experience. The default content of the file looks something like this:

```json
{
  "MicrosoftAppId": "",
  "MicrosoftAppPassword": "",
  "BotBasePath": "/api",
  "BotMessagesPath": "/messages",
  "AzureTableStorageConnectionString": "",
  "RejectConnectionRequestIfNoAggregationChannel": true,
  "PermittedAggregationChannels": "",
  "NoDirectConversationsWithChannels": "emulator, facebook, skype, msteams, webchat"
}
```

### Settings ###

* `BotBasePath` and `BotMessagesPath` can be used to define the messaging endpoint of the bot. The
  endpoint by default is `http(s)://<bot URL>/api/messages`.
* `RejectConnectionRequestIfNoAggregationChannel` defines whether to reject connection requests
  automatically if no aggregation channel is set or not. Pretty straightforward, don't you think?
* `PermittedAggregationChannels` can be used to rule out certain channels as aggregation channels.
  If, for instance, the list contains `facebook`, the bot will refuse to set any Facebook
  conversation as an aggregation channel.
* The solution allows the bot to try and create direct conversations with the accepted "customers".
  `NoDirectConversationsWithChannels` defines the channels where the bot should not try to do this.

### Credentials ###

* `MicrosoftAppId` and `MicrosoftAppPassword` should contain the bot's credentials, which you
  acquire from the [Azure portal](https://portal.azure.com) when you publish the bot.
* The bot needs to have a centralized storage for routing data. When you insert a valid Azure Table
  Storage connection string as the value of the `AzureTableStorageConnectionString` property, the
  storage automatically taken in use.

## Testing the hand-off ##

This scenario utilizes an aggregation concept (see the terminology table in this document). One or
more channels act as aggregated channels where the customer requests (for human assistance) are
sent. The conversation owners (e.g. customer service agents) then accept or reject the requests.

Once you have published the bot, go to the channel you want to receive the requests and issue the
following command to the bot (given that you haven't changed the default bot command handler or the
command itself):

```
@<bot name> watch
```

In case mentions are not supported, you can also use the command keyword:

```
command watch
```

Now all the requests from another channels are forwarded to this channel.
See the default flow below:

| Teams | Slack |
| ----- | ----- |
| ![Setting the aggregation channel](Documentation/Screenshots/msteams-1-watch.png?raw=true) | |
| | ![Connection request sent](/Documentation/Screenshots/slack-1-connection-request.png?raw=true) | |
| ![Connection request accepted](/Documentation/Screenshots/msteams-2-accept-connection-request.png?raw=true) | |
| ![Conversation in Teams](/Documentation/Screenshots/msteams-3-conversation.png?raw=true) | ![Conversation in Slack](/Documentation/Screenshots/slack-2-conversation.png?raw=true) |

### Commands ###

The bot comes with a simple command handling mechanism, which supports the commands in the table
below.

| Command | Description |
| ------- | ----------- |
| `showOptions` | Displays the command options as a card with buttons (convenient!) |
| `Watch` | Marks the current channel as **aggregation** channel (where requests are sent). |
| `Unwatch` | Removes the current channel from the list of aggregation channels. |
| `GetRequests` | Lists all pending connection requests. |
| `AcceptRequest <user ID>` | Accepts the conversation connection request of the given user. If no user ID is entered, the bot will render a nice card with accept/reject buttons given that pending connection requests exist. |
| `RejectRequest <user ID>` | Rejects the conversation connection request of the given user. If no user ID is entered, the bot will render a nice card with accept/reject buttons given that pending connection requests exist. |
| `Disconnect` | Ends the current conversation with a user. |

To issue a command use the bot name:

```
@<bot name> <command> <optional parameters>
```

In case mentions are not supported, you can also use the command keyword:

```
command <command> <optional parameters>
```

Although not an actual command, typing `human` will initiate a connection request, which an agent
can then reject or accept.

## Implementation ##

The core message routing functionality comes from the
[Bot Message Routing (component)](https://github.com/tompaana/bot-message-routing) project.
This sample demonstrates how to use the component and provides the necessary "plumbing" such as
command handling. Here are the main classes of the sample:

* **[HandoffMiddleware](/IntermediatorBotSample/Middleware/HandoffMiddleware.cs)**: Contains all the
  components (class instances) required by the hand-off and implements the main logic flow. This
  middleware class will check every incoming message for hand-off related actions.
* **[CommandHandler](/IntermediatorBotSample/CommandHandling/CommandHandler.cs)**:
  Provides implementation for checking and acting on commands in messages before they are passed to
  a dialog etc.
* **[MessageRouterResultHandler](/IntermediatorBotSample/MessageRouting/MessageRouterResultHandler.cs)**:
  Handles the results of the operations executed by the **`MessageRouter`** of the Bot Message
  Routing component.
* **[ConnectionRequestHandler](/IntermediatorBotSample/MessageRouting/ConnectionRequestHandler.cs)**:
  Implements the main logic for accepting or rejecting connection requests.

## What if I want to have a custom agent portal/channel? ##

Well, right now you have to implement it. There are couple of different ways to go about it. It's
hard to say which one is the best, but if I were to do it, I'd propably start by...

1. ...implementing a REST API endpoint
   (see for instance [Create a Web API with ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-2.1)).
   Then I'd hook the REST API into the message routing code and finally removed the text based
   command handling altogether.
2. Another options is to use back channel messages and hook them up into the current command
   pipeline. Granted some changes would need to be made to separate the direct commands from the
   back channel ones. Also, the response would likely need to be (or recommended to be) in JSON.
3. Something else - remember, it's just a web app!

## See also ##

* [Bot Message Routing (component) project](https://github.com/tompaana/bot-message-routing)
    * [NuGet package](https://www.nuget.org/packages/BotMessageRouting)
* [Chatbots as Middlemen blog post](https://tompaana.github.io/content/chatbots_as_middlemen.html)
