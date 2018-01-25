using IntermediatorBot.Strings;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Globalization;
using Underscore.Bot.Models;

namespace IntermediatorBotSample.CommandHandling
{
    /// <summary>
    /// An utility class for creating command related cards.
    /// </summary>
    public class CommandCardFactory
    {
        /// <summary>
        /// Creates a card with all command options.
        /// </summary>
        /// <param name="botName">The name of the bot (optional).</param>
        /// <returns>A newly created command options card.</returns>
        public static HeroCard CreateCommandOptionsCard(string botName)
        {
            HeroCard card = new HeroCard()
            {
                Title = ConversationText.CommandMenuTitle,
                Subtitle = ConversationText.CommandMenuDescription,

                Text = string.Format(
                    ConversationText.CommandMenuInstructions,
                    Commands.CommandKeyword,
                    botName,
                    Command.ResolveFullCommand(botName, Commands.CommandAcceptRequest, new string[] { "<user ID>" })),

                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandAddAggregationChannel),
                        Type = ActionTypes.ImBack,
                        Value = Command.ResolveFullCommand(botName, Commands.CommandAddAggregationChannel)
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandRemoveAggregationChannel),
                        Type = ActionTypes.ImBack,
                        Value = Command.ResolveFullCommand(botName, Commands.CommandRemoveAggregationChannel)
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandAcceptRequest),
                        Type = ActionTypes.ImBack,
                        Value = Command.ResolveFullCommand(botName, Commands.CommandAcceptRequest)
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandRejectRequest),
                        Type = ActionTypes.ImBack,
                        Value = Command.ResolveFullCommand(botName, Commands.CommandRejectRequest)
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandDisconnect),
                        Type = ActionTypes.ImBack,
                        Value = Command.ResolveFullCommand(botName, Commands.CommandDisconnect)
                    }
#if DEBUG
                    ,
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandDeleteAllRoutingData),
                        Type = ActionTypes.ImBack,
                        Value = Command.ResolveFullCommand(botName, Commands.CommandDeleteAllRoutingData)
                    },
                    new CardAction()
                    {
                        Title = $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandList)} {Commands.CommandParameterParties}",
                        Type = ActionTypes.ImBack,
                        Value = Command.ResolveFullCommand(botName, Commands.CommandList, new string[] { Commands.CommandParameterParties })
                    },
                    new CardAction()
                    {
                        Title = $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandList)} {Commands.CommandParameterRequests}",
                        Type = ActionTypes.ImBack,
                        Value = Command.ResolveFullCommand(botName, Commands.CommandList, new string[] { Commands.CommandParameterRequests })
                    },
                    new CardAction()
                    {
                        Title = $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandList)} {Commands.CommandParameterConnections}",
                        Type = ActionTypes.ImBack,
                        Value = Command.ResolveFullCommand(botName, Commands.CommandList, new string[] { Commands.CommandParameterConnections })
                    },
                    new CardAction()
                    {
                        Title = $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandList)} {Commands.CommandParameterResults}",
                        Type = ActionTypes.ImBack,
                        Value = Command.ResolveFullCommand(botName, Commands.CommandList, new string[] { Commands.CommandParameterResults })
                    }
#endif
                }
            };

            return card;
        }

        /// <summary>
        /// Creates a connection (e.g. human agent) request card.
        /// </summary>
        /// <param name="requestorParty">The party who requested a connection.</param>
        /// <param name="botName">The name of the bot (optional).</param>
        /// <returns>A newly created request card.</returns>
        public static HeroCard CreateRequestCard(Party requestorParty, string botName = null)
        {
            if (requestorParty.ChannelAccount == null)
            {
                throw new ArgumentNullException("The channel account of the requestor is null");
            }

            string requestorChannelAccountName = string.IsNullOrEmpty(requestorParty.ChannelAccount.Name)
                ? StringAndCharConstants.NoUserNamePlaceholder : requestorParty.ChannelAccount.Name;

            string requestorChannelId = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(requestorParty.ChannelId);
            string requestorChannelAccountId = requestorParty.ChannelAccount.Id;

            string acceptCommand =
                Command.ResolveFullCommand(
                    botName, Commands.CommandAcceptRequest, new string[] { requestorChannelAccountId });

            string rejectCommand =
                Command.ResolveFullCommand(
                    botName, Commands.CommandRejectRequest, new string[] { requestorChannelAccountId });

            HeroCard card = new HeroCard()
            {
                Title = ConversationText.ConnectionRequestTitle,
                Subtitle = string.Format(ConversationText.RequestorDetailsTitle, requestorChannelAccountName, requestorChannelId),
                Text = string.Format(ConversationText.AcceptRejectConnectionHint, acceptCommand, rejectCommand),

                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = ConversationText.AcceptButtonTitle,
                        Type = ActionTypes.ImBack,
                        Value = acceptCommand
                    },
                    new CardAction()
                    {
                        Title = ConversationText.RejectButtonTitle,
                        Type = ActionTypes.ImBack,
                        Value = rejectCommand
                    }
                }
            };

            return card;
        }

        /// <summary>
        /// Creates multiple request cards to be used e.g. in a carousel.
        /// </summary>
        /// <param name="requestorParties">The list of requestor parties (pending requests).</param>
        /// <param name="botName">The name of the bot (optional).</param>
        /// <returns>A list of request cards as attachments.</returns>
        public static IList<Attachment> CreateMultipleRequestCards(IList<Party> requestorParties, string botName)
        {
            IList<Attachment> attachments = new List<Attachment>();

            foreach (Party requestorParty in requestorParties)
            {
                if (requestorParty.ChannelAccount != null)
                {
                    attachments.Add(CreateRequestCard(requestorParty, botName).ToAttachment());
                }
            }

            return attachments;
        }

        /// <summary>
        /// Creates a card for accepting/rejecting multiple requests.
        /// </summary>
        /// <param name="requestorParties">The list of requestor parties (pending requests).</param>
        /// <param name="doAccept">If true, will create an accept card. If false, will create a reject card.</param>
        /// <param name="botName">The name of the bot (optional).</param>
        /// <returns>The newly created card.</returns>
        public static HeroCard CreateAcceptOrRejectCardForMultipleRequests(IList<Party> requestorParties, bool doAccept, string botName)
        {
            HeroCard card = new HeroCard()
            {
                Title = (doAccept
                    ? ConversationText.AcceptConnectionRequestsCardTitle
                    : ConversationText.RejectConnectionRequestCardTitle),
                Subtitle = (doAccept
                    ? ConversationText.AcceptConnectionRequestsCardInstructions
                    : ConversationText.RejectConnectionRequestsCardInstructions),
            };

            string command = null;
            card.Buttons = new List<CardAction>();

            if (!doAccept && requestorParties.Count > 1)
            {
                card.Buttons.Add(new CardAction()
                {
                    Title = ConversationText.RejectAll,
                    Type = ActionTypes.ImBack,
                    Value = Command.ResolveFullCommand(
                        botName, Commands.CommandRejectRequest, new string[] { Commands.CommandParameterAll })
                });
            }

            foreach (Party requestorParty in requestorParties)
            {
                if (requestorParty.ChannelAccount == null)
                {
                    throw new ArgumentNullException("The channel account of the requestor is null");
                }

                string requestorChannelAccountName = string.IsNullOrEmpty(requestorParty.ChannelAccount.Name)
                    ? StringAndCharConstants.NoUserNamePlaceholder : requestorParty.ChannelAccount.Name;

                string requestorChannelId = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(requestorParty.ChannelId);
                string requestorChannelAccountId = requestorParty.ChannelAccount.Id;

                command = Command.ResolveFullCommand(
                    botName,
                    (doAccept ? Commands.CommandAcceptRequest : Commands.CommandRejectRequest),
                    new string[] { requestorChannelAccountId });

                card.Buttons.Add(new CardAction()
                {
                    Title = string.Format(
                        ConversationText.RequestorDetailsItem,
                        requestorChannelAccountName,
                        requestorChannelId,
                        requestorChannelAccountId),
                    Type = ActionTypes.ImBack,
                    Value = command
                });
            }

            return card;
        }

        /// <summary>
        /// Adds the given card into the given activity as an attachment.
        /// </summary>
        /// <param name="activity">The activity to add the card into.</param>
        /// <param name="card">The card to add.</param>
        /// <returns>The given activity with the card added.</returns>
        public static Activity AddCardToActivity(Activity activity, HeroCard card)
        {
            activity.Attachments = new List<Attachment>() { card.ToAttachment() };
            return activity;
        }
    }
}