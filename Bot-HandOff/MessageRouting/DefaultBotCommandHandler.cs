using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MessageRouting
{
    /// <summary>
    /// The default handler for bot commands related to message routing.
    /// </summary>
    public class DefaultBotCommandHandler : IBotCommandHandler
    {
        private IRoutingDataManager _routingDataManager;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="routingDataManager">The routing data manager.</param>
        public DefaultBotCommandHandler(IRoutingDataManager routingDataManager)
        {
            _routingDataManager = routingDataManager;
        }

        /// <summary>
        /// All messages where the bot was mentioned ("@<bot name>) are checked for possible commands.
        /// See IBotCommandHandler.cs for more information.
        /// </summary>
        public async virtual Task<bool> HandleCommandAsync(Activity activity)
        {
            bool wasHandled = false;
            Activity replyActivity = null;

            // Sole use of mentions here is unreliable for Skype & Webchat
            // Therefore just parse the text for any bot id reference
            if (WasBotAddressedDirectly(activity)
                || activity.Text.Contains(activity.Recipient.Name)
                || (!string.IsNullOrEmpty(activity.Text) && activity.Text.StartsWith($"{Commands.CommandKeyword} ")))
            {
                //activity.RemoveRecipientMention(); not working here
                //string message = MessagingUtils.StripMentionsFromMessage(activity);
                string message = activity.Text.Replace($"@{activity.Recipient.Name}", "").Trim();
                if (message.StartsWith($"{Commands.CommandKeyword} "))
                {
                    message = message.Remove(0, Commands.CommandKeyword.Length + 1);
                }

                switch (message?.ToLower())
                {
                    case string s when (s.StartsWith(Commands.CommandEndEngagement)):
                        {
                            // End the 1:1 conversation
                            Party senderParty = MessagingUtils.CreateSenderParty(activity);

                            if (await MessageRouterManager.Instance.EndEngagementAsync(senderParty) == false)
                            {
                                replyActivity = activity.CreateReply("Failed to end the engagement");
                            }
                            wasHandled = true;
                            break;
                        }
                    case string s when (s.StartsWith(Commands.CommandAddAggregationChannel)):
                        {
                            // Check if the Aggregation Party already exists
                            /* not a specific user, but a channel/conv */
                            Party aggregationParty = new Party(activity.ServiceUrl, activity.ChannelId, null, activity.Conversation);

                            // Establish the sender's channel/conversation as an aggreated one if not already exists
                            if (_routingDataManager.AddAggregationParty(aggregationParty))
                            {
                                replyActivity = activity.CreateReply("This channel/conversation is now where the requests are aggregated");
                            }
                            else
                            {
                                replyActivity = activity.CreateReply("This channel/conversation is already receiving requests");
                            }
                            wasHandled = true;
                            break;
                        }
                    case string s when (s.StartsWith(Commands.CommandAcceptRequest) || s.StartsWith(Commands.CommandRejectRequest)):
                        {
                            // Accept/reject conversation request
                            bool doAccept = s.StartsWith(Commands.CommandAcceptRequest);
                            Party senderParty = MessagingUtils.CreateSenderParty(activity);

                            if (_routingDataManager.IsAssociatedWithAggregation(senderParty))
                            {
                                // The party is associated with the aggregation and has the right to accept/reject
                                Party senderInConversation =
                                    _routingDataManager.FindEngagedPartyByChannel(senderParty.ChannelId, senderParty.ChannelAccount);
                                replyActivity = await GetEngagementActivity(activity, replyActivity, message, doAccept, senderParty, senderInConversation);
                                wasHandled = true;
                            }
                            break;
                        }
                    case string s when (s.StartsWith(Commands.CommandListOptions)):
                        {
                            replyActivity = await GetAvaialableAgentOptionsCard(activity);
                            wasHandled = true;
                            break;
                        }

                    #region Agent debugging commands

                    // TODO: Remove from production
                    case string s when (s.StartsWith(Commands.CommandDeleteAllRoutingData)):
                        {
                            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Deleting all data..."));
                            _routingDataManager.DeleteAll();
                            wasHandled = true;
                            break;
                        }
                    case string s when (s.StartsWith(Commands.CommandListAllParties)):
                        {
                            string replyMessage = string.Empty;
                            string parties = string.Empty;

                            foreach (Party userParty in _routingDataManager.GetUserParties())
                            {
                                parties += userParty.ToString() + "\n";
                            }

                            if (string.IsNullOrEmpty(parties))
                            {
                                replyMessage = "No user parties;\n";
                            }
                            else
                            {
                                replyMessage = "Users:\n" + parties;
                            }

                            parties = string.Empty;

                            foreach (Party botParty in _routingDataManager.GetBotParties())
                            {
                                parties += botParty.ToString() + "\n";
                            }

                            if (string.IsNullOrEmpty(parties))
                            {
                                replyMessage += "No bot parties";
                            }
                            else
                            {
                                replyMessage += "Bot:\n" + parties;
                            }

                            replyActivity = activity.CreateReply(replyMessage);
                            wasHandled = true;
                            break;
                        }
                    case string s when (s.StartsWith(Commands.CommandListPendingRequests)):
                        {
                            var attachments = new List<Attachment>();
                            
                            foreach (Party party in _routingDataManager.GetPendingRequests())
                            {
                                attachments.Add(MessagingUtils.GetAgentRequestHeroCard(party.ChannelAccount.Name, party.ChannelId, party.ChannelAccount.Id, party));
                            }

                            replyActivity = activity.CreateReply("No pending requests");
                            if (attachments.Count > 0)
                            {
                                replyActivity.Text = $"{attachments.Count} Pending requests found:";
                                replyActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                replyActivity.Attachments = attachments;
                            }
                            wasHandled = true;
                            break;
                        }
                    case string s when (s.StartsWith(Commands.CommandListEngagements)):
                        {
                            string parties = _routingDataManager.EngagementsAsString();

                            if (string.IsNullOrEmpty(parties))
                            {
                                replyActivity = activity.CreateReply("No conversations");
                            }
                            else
                            {
                                replyActivity = activity.CreateReply(parties);
                            }

                            wasHandled = true;
                            break;
                        }
#if DEBUG
                    case string s when (s.StartsWith(Commands.CommandListLastMessageRouterResults)):
                        {
                            LocalRoutingDataManager routingDataManager =
                                (MessageRouterManager.Instance.RoutingDataManager as LocalRoutingDataManager);

                            if (routingDataManager != null)
                            {
                                string resultsAsString = routingDataManager.GetLastMessageRouterResults();
                                replyActivity = activity.CreateReply($"{(string.IsNullOrEmpty(resultsAsString) ? "No results" : resultsAsString)}");
                                wasHandled = true;
                            }
                            break;
                        }
#endif

                    #endregion
                    default:
                        replyActivity = activity.CreateReply($"Command \"{message}\" not recognized");
                        break;
                }

                if (replyActivity != null)
                {
                    ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    await connector.Conversations.ReplyToActivityAsync(replyActivity);
                }
            }

            return wasHandled;
        }

        /// <summary>
        /// Checks the given activity and determines whether the message was addressed directly to
        /// the bot or not.
        /// 
        /// Note: Only mentions are inspected at the moment.
        /// </summary>
        /// <param name="messageActivity">The message activity.</param>
        /// <returns>True, if the message was address directly to the bot. False otherwise.</returns>
        protected bool WasBotAddressedDirectly(IMessageActivity messageActivity)
        {
            bool botWasMentioned = false;
            Mention[] mentions = messageActivity.GetMentions();

            foreach (Mention mention in mentions)
            {
                foreach (Party botParty in _routingDataManager.GetBotParties())
                {
                    if (mention.Mentioned.Id.Equals(botParty.ChannelAccount.Id))
                    {
                        botWasMentioned = true;
                        break;
                    }
                }
            }

            return botWasMentioned;
        }


        private async Task<Activity> GetEngagementActivity(Activity activity, Activity replyActivity, string message, bool doAccept, Party senderParty, Party senderInConversation)
        {
            if (senderInConversation == null || !_routingDataManager.IsEngaged(senderInConversation, EngagementProfile.Owner))
            {
                if (_routingDataManager.GetPendingRequests().Count > 0)
                {
                    // The name of the user to accept should be the second word
                    string[] splitMessage = message.Split(' ');

                    if (splitMessage.Count() > 1 && !string.IsNullOrEmpty(splitMessage[1]))
                    {
                        Party partyToAcceptOrReject = null;
                        string errorMessage = "";

                        try
                        {
                            partyToAcceptOrReject = _routingDataManager.GetPendingRequests().Single(
                                  party => (party.ChannelAccount != null
                                      && !string.IsNullOrEmpty(party.ChannelAccount.Id)
                                      && party.ChannelAccount.Id.Equals(splitMessage[1])));
                        }
                        catch (InvalidOperationException e)
                        {
                            errorMessage = e.Message;
                        }

                        if (partyToAcceptOrReject != null)
                        {
                            MessageRouterManager messageRouterManager = MessageRouterManager.Instance;

                            if (doAccept)
                            {
                                await messageRouterManager.AddEngagementAsync(senderParty, partyToAcceptOrReject);
                            }
                            else
                            {
                                await messageRouterManager.RejectPendingRequestAsync(partyToAcceptOrReject, senderParty);
                            }
                        }
                        else
                        {
                            replyActivity = activity.CreateReply(
                                $"Could not find a pending request for user {splitMessage[1]}; {errorMessage}");
                        }
                    }
                    else
                    {
                        replyActivity = activity.CreateReply("User name missing");
                    }
                }
                else
                {
                    replyActivity = activity.CreateReply("No pending requests");
                }
            }
            else
            {
                Party otherParty = _routingDataManager.GetEngagedCounterpart(senderInConversation);

                if (otherParty != null)
                {
                    replyActivity = activity.CreateReply($"You are already engaged in a conversation with {otherParty.ChannelAccount.Name}");
                }
                else
                {
                    replyActivity = activity.CreateReply("An error occured");
                }
            }

            return replyActivity;
        }

        private async Task<Activity> GetAvaialableAgentOptionsCard(Activity activity)
        {

            Activity messageActivity = activity.CreateReply();
            string commandKeyword = $"{Commands.CommandKeyword}/@{activity.Recipient.Name}";
            string acceptCommand = $"{Commands.CommandKeyword} {Commands.CommandAcceptRequest} *requestId*";
            string rejectCommand = $"{Commands.CommandKeyword} {Commands.CommandRejectRequest} *requestId*";

            HeroCard thumbnailCard = new HeroCard()
            {
                Title = "Agent menu",
                Subtitle = "Agent/supervisor options for controlling end user bot conversations",
                Text = $"Select from the buttons below.\n\nOr type \"{commandKeyword}\" followed by the keyword, eg. \"{acceptCommand}\"",
                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = "Watch",
                        Type = ActionTypes.PostBack,
                        Value = Commands.CommandAddAggregationChannel
                    },
                    new CardAction()
                    {
                        Title = "Connect",
                        Type = ActionTypes.PostBack,
                        Value = acceptCommand
                    },
                    new CardAction()
                    {
                        Title = "Disconnect",
                        Type = ActionTypes.PostBack,
                        Value = Commands.CommandEndEngagement
                    },
                    //new CardAction()
                    //{
                    //    Title = "Reject",
                    //    Type = ActionTypes.PostBack,
                    //    Value = rejectCommand
                    //},
                    new CardAction()
                    {
                        Title = "List active",
                        Type = ActionTypes.PostBack,
                        Value =  $"{Commands.CommandKeyword} {Commands.CommandListEngagements}"
                    },
                    new CardAction()
                    {
                        Title = "List waiting",
                        Type = ActionTypes.PostBack,
                        Value =  $"{Commands.CommandKeyword} {Commands.CommandListPendingRequests}"
                    },
                    new CardAction()
                    {
                        Title = "List all",
                        Type = ActionTypes.PostBack,
                        Value =  $"{Commands.CommandKeyword} {Commands.CommandListAllParties}"
                    },

                    new CardAction()
                    {
                        Title = "Reset",
                        Type = ActionTypes.PostBack,
                        Value =  Commands.CommandDeleteAllRoutingData
                    }
                }
            };

            messageActivity.Attachments = new List<Attachment>() { thumbnailCard.ToAttachment() };
            return messageActivity;
        }

    }
}
