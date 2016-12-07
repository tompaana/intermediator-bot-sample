# Intermediator Bot Sample #

A bot build on [Microsoft Bot Framework](https://dev.botframework.com/) that
routes messages between two users on different channels.

A possible use case for this type of a bot would be a customer service scenario
where the bot relays the messages between a customer and a customer service agent.


### Running and testing ###

To test the bot, publish it and connect it to the channels of your choice.
If you are new to bots, please familiarize yourself first with the basics
[here](https://dev.botframework.com/).

I used [Microsoft Bot Framework Emulator](https://docs.botframework.com/en-us/tools/bot-framework-emulator/)
and Slack for testing the bot. To communicate with a remotely hosted bot, you
can use [ngrok](https://ngrok.com/) tunneling software:

1. In emulator open **App Settings**
2. Make sure ngrok path is set and check the local host port under **Callback URL**
3. Launch ngrok: `ngrok http -host-header=rewrite <local host port>`
4. Set the bot end point in emulator (`https://<bot URL>/api/messages`)
5. Set **Microsoft App ID** and **Microsoft App Password**

See also: [Microsoft Bot Framework Emulator wiki](https://github.com/microsoft/botframework-emulator/wiki/Getting-Started)

#### The flow ####

| Emulator with ngrok | Slack |
| ------------------- | ----- |
| | ![Initialization](Documentation/Screenshots/Initialization.png?raw=true) |
| ![Request sent](/Documentation/Screenshots/RequestSent.png?raw=true) | ![Request accepted](/Documentation/Screenshots/RequestAccepted.png?raw=true) |
| | ![Direct messaging channel created](/Documentation/Screenshots/DirectMessagingChannelCreated.png?raw=true) |
| ![Conversation in emulator](/Documentation/Screenshots/ConversationInEmulator.png?raw=true) | ![Conversation in Slack](/Documentation/Screenshots/ConversationInSlack.png?raw=true) |


### Implementation ###

TBD

### Acknowledgements ###

Although you can't see it in the change history,
[Edouard Mathon](https://github.com/edouard-mathon) added
multi-aggregation-channel support (many thanks!), which in human talk means that
the chat requests can be received by multiple channels/conversations.
