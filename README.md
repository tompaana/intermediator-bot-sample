# Intermediator Bot Sample #

[![Build status](https://ci.appveyor.com/api/projects/status/i1u8puahyxl79ha6?svg=true)](https://ci.appveyor.com/project/tompaana/intermediator-bot-sample)

This is a sample bot, built with the [Microsoft Bot Framework](https://dev.botframework.com/) (v4),
that routes messages between two users on different channels. This sample utilizes the
[Bot Message Routing (component) project](https://github.com/tompaana/bot-message-routing).

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
* [Testing the hand-off](#testing-the-handoff)

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

### Deploying the bot ###

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

### Testing the hand-off ###

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
| `AcceptRequest <user ID>` | Accepts the conversation connection request of the given user. |
| `RejectRequest <user ID>` | Rejects the conversation connection request of the given user. |
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
command handling.

### Key classes of the sample ###

**Command handling**

* **[BackChannelMessageHandler](/IntermediatorBotSample/CommandHandling/BackChannelMessageHandler.cs)**:
  Provides implementation for checking and acting on back channel (command) messages. Back channel
  messages are used by the agent UI.

* **[CommandMessageHandler](/IntermediatorBotSample/CommandHandling/CommandMessageHandler.cs)**:
  Provides implementation for checking and acting on commands in messages before they are passed to
  a dialog etc.

**Controllers**

* **[AgentController](/IntermediatorBotSample/Controllers/AgentController.cs)**:
  A controller for the agent UI. Enables the agent UI to check the status of pending requests and
  automatically accept them.

* **[MessagesController](/IntermediatorBotSample/Controllers/MessagesController.cs)**:
  This class is included in the bot project template. In this sample it is beneficial to look into
  how to the command handling and message routing implementations integrate into the bot code
  (see the [`Post`](https://github.com/tompaana/intermediator-bot-sample/blob/dd9c6ff2e81dfc1037295d3f67065df4ed39bbc0/IntermediatorBotSample/Controllers/MessagesController.cs#L29) method).

**Message routing (utils)**

* **[MessageRouterResultHandler](/IntermediatorBotSample/MessageRouting/MessageRouterResultHandler.cs)**:
  Handles the results of the operations executed by `MessageRouterManager`.

### App settings and credentials ###

App settings and credentials are available in the [Web.config](/IntermediatorBotSample/Web.config)
file of this sample. The settings can be used to tailor the experience.

#### Credentials ####

The credentials (and the bot ID) can be placed either directly in the `Web.config` file (**not
recommended** when the code is managed in a repository to avoid accidentally leaking them there) or
in the separate `AppSettingsAndCredentials.config` file in the `IntermediatorBotSample` folder of
the project (**recommended**). The content of the `AppSettingsAndCredentials.config` file is added
into the `Web.config` when the project is built. The format of the
`AppSettingsAndCredentials.config` file is as follows:

```xml
<appSettings>
  <!-- Update these with your BotId, Microsoft App Id and your Microsoft App Password-->
  <add key="BotId" value="" />
  <add key="MicrosoftAppId" value="" />
  <add key="MicrosoftAppPassword" value="" />

  <!-- Add your connection string for routing data storage below -->
  <add key="RoutingDataStorageConnectionString" value="" />
</appSettings>
```

Note that since the `AppSettingsAndCredentials.config` file is not included in the repository,
**you must create the file**.
A [template file](/IntermediatorBotSample/AppSettingsAndCredentials.config.template) is provided
for your convenience. Simply remove the `.template` postfix and fill in the details.

#### Settings ####

**RejectConnectionRequestIfNoAggregationChannel**: This setting, which is set to true by default,
will cause the `IRoutingDataManager` implementation to return the `NoAgentsAvailable` result when no
agents are watching for incoming requests. You can then send an appropriate response to let the user
know no agents are available within the implementation of your `MessageRouterResult` handler.
If this is set to `false`, then the `IRoutingDataManager` implementation will process and add
the user's request to the pending requests list and return the `ConnectionRequested` result instead.

```xml
<add key="RejectConnectionRequestIfNoAggregationChannel" value="false" />
```

**PermittedAggregationChannels**: If you wish to only allow conversation owners (i.e. customer
service agent) to use a specific channel or channels, you can specify a comma separated list of
channel IDs here.  This will prevent agent commands from being used on other channels and prevent
users from accidentally or deliberately calling such commands. If you leave the value empty, all
channels are considered permitted. If, for instance, you wanted to restrict the agents to use the
emulator and Skype channels, you would use:

```xml
<add key="PermittedAggregationChannels" value="emulator,skype" />
```

The provided [BotSettings](/IntermediatorBotSample/Settings/BotSettings.cs) utility class can be
used to easily access the settings in the code.

## See also ##

* [Bot Message Routing (component) project](https://github.com/tompaana/bot-message-routing)
    * [NuGet package](https://www.nuget.org/packages/BotMessageRouting)
* [Chatbots as Middlemen blog post](http://tomipaananen.azurewebsites.net/?p=1851)
