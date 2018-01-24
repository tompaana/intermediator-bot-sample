# Intermediator Bot Sample #

[![Build status](https://ci.appveyor.com/api/projects/status/i1u8puahyxl79ha6?svg=true)](https://ci.appveyor.com/project/tompaana/intermediator-bot-sample)

A bot build on [Microsoft Bot Framework](https://dev.botframework.com/) that routes messages between
two users on different channels. This is sample utilizes the core functionality found in
[Bot Message Routing (component) project](https://github.com/tompaana/bot-message-routing).

A possible use case for this type of a bot would be a customer service scenario where the bot relays
the messages between a customer and a customer service agent.

This is a C# sample - if you're looking to do this with Node, see
[this sample](https://github.com/palindromed/Bot-HandOff).

## Getting started ##

Since this is an advanced bot scenario, the prerequisites include that you are familiar with the
basic concepts of the Microsoft Bot Framework and you know the C# programming language. Before
getting started it is recommended that you have installed the following tools:

* [Visual Studio IDE](https://www.visualstudio.com/vs/)
* [ngrok](https://ngrok.com/)
* [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator) ([download](https://github.com/Microsoft/BotFramework-Emulator/releases))
    * [Debug bots with the Bot Framework Emulator](https://docs.microsoft.com/fi-fi/bot-framework/bot-service-debug-emulator)

### Publishing the bot ###

This sample demonstrates routing messages between different users on different channels. Hence,
using only the emulator to test the sample may prove difficult. To utilize other channels, you must
first publish the bot:

1. Go to the [Azure Portal](https://portal.azure.com)
2. Create a new **Bot Service** (**Web App Bot** is the recommended type for this sample)
3. Collect the **application ID** and **application password** of the newly created Bot Service
4. Copy and rename the application settings and credentials template file ([AppSettingsAndCredentials.config.template](/IntermediatorBotSample/AppSettingsAndCredentials.config.template)) by removing the `.template` postfix
5. Open the solution file ([IntermediatorBotSample.sln](/IntermediatorBotSample.sln)) in Visual Studio
6. Insert the settings and credentials into `AppSettingsAndCredentials.config` file
    * See [App settings and credentials](#app-settings-and-credentials) section for more details
7. Build the solution and publish (right-click the **IntermediatorBotSample** project in the **Solution Explorer** in Visual Studio and select **Publish**)
    * For more details, see [Publish a bot to Bot Service](https://docs.microsoft.com/en-us/bot-framework/bot-service-continuous-deployment)

### Scenario 1: Channel <-> channel ###

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

| Emulator with ngrok | Slack |
| ------------------- | ----- |
| | ![Initialization](Documentation/Screenshots/Initialization.png?raw=true) |
| ![Request sent](/Documentation/Screenshots/RequestSent.png?raw=true) | ![Request accepted](/Documentation/Screenshots/RequestAccepted.png?raw=true) |
| | ![Direct messaging channel created](/Documentation/Screenshots/DirectMessagingChannelCreated.png?raw=true) |
| ![Conversation in emulator](/Documentation/Screenshots/ConversationInEmulator.png?raw=true) | ![Conversation in Slack](/Documentation/Screenshots/ConversationInSlack.png?raw=true) |

### Scenario 2: Channel <-> call center (agent UI) ###

**BETA - [Help wanted!](https://github.com/tompaana/intermediator-bot-sample/issues/27)**

In this scenario the conversation owners (e.g. customer service agents) access the bot via the
webchat component, [Agent UI](https://github.com/billba/agent), implemented by
[Bill Barnes](https://github.com/billba). Each customer request (for human assistance) automatically
opens a new chat window in the agent UI.

| Emulator | Agent UI |
| -------- | -------- |
| ![Emulator](Documentation/Screenshots/ConversationInEmulatorWithAgentUI.png?raw=true) | ![Agent UI](Documentation/Screenshots/AgentUI.png?raw=true) |

To set this up, follow these steps:

0. Make sure you have [Node.js](https://nodejs.org) installed
1. Clone or download [the Agent UI repository](https://github.com/billba/agent)
2. Inside `index.ts`, update the line below with your bot's endpoint:

    `fetch("http://YOUR_BOT_ENDPOINT/api/agent/1")`
    
    Example: `fetch("http://mybot.azurewebsites.net/api/agent/1")`

3. Inside `index.ts`, update the line below with your bot secret key (which you can find in your **Bot Service**)

    ```js
    iframe.src = 'botchat?s=YOUR_DIRECTLINE_SECRET_ID';
    ```

4. Run `npm install` to get the npm packages 

    * You only need to run this command once, unless you add other node packages to the project
    * If you encounter `error TS2300`, run `npm install typescript@2.0.10`

5. Run `npm run build` to build the app 

    * You need to run this every time you make changes to the code before you start the application

6. Run `npm run start` to start the app
7. Go to http://localhost:8080 to see the Agent UI

#### Troubleshooting agent UI scenario ####

Make sure that the value of `RejectConnectionRequestIfNoAggregationChannel` key in
[Web.config](/IntermediatorBotSample/Web.config) is `false`:

```xml
<add key="RejectConnectionRequestIfNoAggregationChannel" value="false" />
```

Otherwise the agent UI will not receive the requests, but they are automatically rejected
(if no aggregation channel is set).

### Commands ###

The bot comes with a simple command handling mechanism, which implements the commands in the table below.

| Command | Description |
| ------- | ----------- |
| `options` | Displays the command options as a card with buttons (convenient!) |
| `watch` | Marks the current channel as **aggregation** channel (where requests are sent). |
| `unwatch` | Removes the current channel from the list of aggregation channels. |
| `accept <user ID>` | Accepts the conversation connection request of the given user. |
| `reject <user ID>` | Rejects the conversation connection request of the given user. |
| `disconnect` | Ends the current conversation with a user. |
| `reset` | Deletes all routing data! *(Enabled only in debug builds)* |
| `list parties` | Lists all parties the bot is aware of. *(Enabled only in debug builds)* |
| `list requests` | Lists all pending requests. *(Enabled only in debug builds)* |
| `list conversations` | Lists all conversations (connections). *(Enabled only in debug builds)* |
| `list results` | Lists all handled results (`MessageRouterResult`). *(Enabled only in debug builds)* |

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
    * [NuGet package](https://www.nuget.org/packages/Underscore.Bot.MessageRouting)
* [Chatbots as Middlemen blog post](http://tomipaananen.azurewebsites.net/?p=1851)
