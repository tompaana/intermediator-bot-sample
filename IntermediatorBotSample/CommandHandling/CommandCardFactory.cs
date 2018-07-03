using IntermediatorBotSample.Resources;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Globalization;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.Models;

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
                Title = Strings.CommandMenuTitle,
                Subtitle = Strings.CommandMenuDescription,

                Text = string.Format(
                    Strings.CommandMenuInstructions,
                    Command.CommandKeyword,
                    botName,
                    new Command(
                        Commands.AcceptRequest,
                        new string[] { "(user ID)", "(user conversation ID)" },
                        botName).ToString()),

                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = Command.CommandToString(Commands.Watch),
                        Type = ActionTypes.ImBack,
                        Value = new Command(Commands.Watch, null, botName).ToString()
                    },
                    new CardAction()
                    {
                        Title = Command.CommandToString(Commands.Unwatch),
                        Type = ActionTypes.ImBack,
                        Value = new Command(Commands.Unwatch, null, botName).ToString()
                    },
                    new CardAction()
                    {
                        Title = Command.CommandToString(Commands.GetRequests),
                        Type = ActionTypes.ImBack,
                        Value = new Command(Commands.GetRequests, null, botName).ToString()
                    },
                    new CardAction()
                    {
                        Title = Command.CommandToString(Commands.AcceptRequest),
                        Type = ActionTypes.ImBack,
                        Value = new Command(Commands.AcceptRequest, null, botName).ToString()
                    },
                    new CardAction()
                    {
                        Title = Command.CommandToString(Commands.RejectRequest),
                        Type = ActionTypes.ImBack,
                        Value = new Command(Commands.RejectRequest, null, botName).ToString()
                    },
                    new CardAction()
                    {
                        Title = Command.CommandToString(Commands.GetHistory),
                        Type = ActionTypes.ImBack,
                        Value = new Command(Commands.GetHistory, null, botName).ToString()
                    },
                    new CardAction()
                    {
                        Title = Command.CommandToString(Commands.Disconnect),
                        Type = ActionTypes.ImBack,
                        Value = new Command(Commands.Disconnect, null, botName).ToString()
                    }
                }
            };

            return card;
        }

        /// <summary>
        /// Creates a large connection request card.
        /// </summary>
        /// <param name="connectionRequest">The connection request.</param>
        /// <param name="botName">The name of the bot (optional).</param>
        /// <returns>A newly created request card.</returns>
        public static HeroCard CreateConnectionRequestCard(
            ConnectionRequest connectionRequest, string botName = null)
        {
            if (connectionRequest == null || connectionRequest.Requestor == null)
            {
                throw new ArgumentNullException("The connection request or the conversation reference of the requestor is null");
            }

            ChannelAccount requestorChannelAccount =
                RoutingDataManager.GetChannelAccount(connectionRequest.Requestor);

            if (requestorChannelAccount == null)
            {
                throw new ArgumentNullException("The channel account of the requestor is null");
            }

            string requestorChannelAccountName = string.IsNullOrEmpty(requestorChannelAccount.Name)
                ? StringConstants.NoUserNamePlaceholder : requestorChannelAccount.Name;
            string requestorChannelId =
                CultureInfo.CurrentCulture.TextInfo.ToTitleCase(connectionRequest.Requestor.ChannelId);

            Command acceptCommand =
                Command.CreateAcceptOrRejectConnectionRequestCommand(connectionRequest, true, botName);
            Command rejectCommand =
                Command.CreateAcceptOrRejectConnectionRequestCommand(connectionRequest, false, botName);

            HeroCard card = new HeroCard()
            {
                Title = Strings.ConnectionRequestTitle,
                Subtitle = string.Format(Strings.RequestorDetailsTitle, requestorChannelAccountName, requestorChannelId),
                Text = string.Format(Strings.AcceptRejectConnectionHint, acceptCommand.ToString(), rejectCommand.ToString()),

                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = Strings.AcceptButtonTitle,
                        Type = ActionTypes.ImBack,
                        Value = acceptCommand.ToString()
                    },
                    new CardAction()
                    {
                        Title = Strings.RejectButtonTitle,
                        Type = ActionTypes.ImBack,
                        Value = rejectCommand.ToString()
                    }
                }
            };

            return card;
        }

        /// <summary>
        /// Creates multiple large connection request cards.
        /// </summary>
        /// <param name="connectionRequests">The connection requests.</param>
        /// <param name="botName">The name of the bot (optional).</param>
        /// <returns>A list of request cards as attachments.</returns>
        public static IList<Attachment> CreateMultipleConnectionRequestCards(
            IList<ConnectionRequest> connectionRequests, string botName = null)
        {
            IList<Attachment> attachments = new List<Attachment>();

            foreach (ConnectionRequest connectionRequest in connectionRequests)
            {
                attachments.Add(CreateConnectionRequestCard(connectionRequest, botName).ToAttachment());
            }

            return attachments;
        }

        /// <summary>
        /// Creates a compact card for accepting/rejecting multiple requests.
        /// </summary>
        /// <param name="connectionRequests">The connection requests.</param>
        /// <param name="doAccept">If true, will create an accept card. If false, will create a reject card.</param>
        /// <param name="botName">The name of the bot (optional).</param>
        /// <returns>The newly created card.</returns>
        public static HeroCard CreateMultiConnectionRequestCard(
            IList<ConnectionRequest> connectionRequests, bool doAccept, string botName = null)
        {
            HeroCard card = new HeroCard()
            {
                Title = (doAccept
                    ? Strings.AcceptConnectionRequestsCardTitle
                    : Strings.RejectConnectionRequestCardTitle),
                Subtitle = (doAccept
                    ? Strings.AcceptConnectionRequestsCardInstructions
                    : Strings.RejectConnectionRequestsCardInstructions),
            };

            card.Buttons = new List<CardAction>();

            if (!doAccept && connectionRequests.Count > 1)
            {
                card.Buttons.Add(new CardAction()
                {
                    Title = Strings.RejectAll,
                    Type = ActionTypes.ImBack,
                    Value = new Command(Commands.RejectRequest, new string[] { Command.CommandParameterAll }, botName).ToString()
                });
            }

            foreach (ConnectionRequest connectionRequest in connectionRequests)
            {
                ChannelAccount requestorChannelAccount =
                    RoutingDataManager.GetChannelAccount(connectionRequest.Requestor, out bool isBot);

                if (requestorChannelAccount == null)
                {
                    throw new ArgumentNullException("The channel account of the requestor is null");
                }

                string requestorChannelAccountName = string.IsNullOrEmpty(requestorChannelAccount.Name)
                    ? StringConstants.NoUserNamePlaceholder : requestorChannelAccount.Name;
                string requestorChannelId =
                    CultureInfo.CurrentCulture.TextInfo.ToTitleCase(connectionRequest.Requestor.ChannelId);
                string requestorChannelAccountId = requestorChannelAccount.Id;

                Command command =
                    Command.CreateAcceptOrRejectConnectionRequestCommand(connectionRequest, doAccept, botName);

                card.Buttons.Add(new CardAction()
                {
                    Title = string.Format(
                        Strings.RequestorDetailsItem,
                        requestorChannelAccountName,
                        requestorChannelId,
                        requestorChannelAccountId),
                    Type = ActionTypes.ImBack,
                    Value = command.ToString()
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
