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
                    new Command(Commands.AcceptRequest, new string[] { "<user ID>" }, botName).ToString()),

                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Command.CommandToString(Commands.Watch)),
                        Type = ActionTypes.ImBack,
                        Value = new Command(Commands.Watch, null, botName).ToString()
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Command.CommandToString(Commands.Unwatch)),
                        Type = ActionTypes.ImBack,
                        Value = new Command(Commands.Unwatch, null, botName).ToString()
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Command.CommandToString(Commands.AcceptRequest)),
                        Type = ActionTypes.ImBack,
                        Value = new Command(Commands.AcceptRequest, null, botName).ToString()
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Command.CommandToString(Commands.RejectRequest)),
                        Type = ActionTypes.ImBack,
                        Value = new Command(Commands.RejectRequest, null, botName).ToString()
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Command.CommandToString(Commands.Disconnect)),
                        Type = ActionTypes.ImBack,
                        Value = new Command(Commands.Disconnect, null, botName).ToString()
                    }
                }
            };

            return card;
        }

        /// <summary>
        /// Creates a connection (e.g. human agent) request card.
        /// </summary>
        /// <param name="requestor">The party who requested a connection.</param>
        /// <param name="botName">The name of the bot (optional).</param>
        /// <returns>A newly created request card.</returns>
        public static HeroCard CreateRequestCard(ConversationReference requestor, string botName = null)
        {
            ChannelAccount requestorChannelAccount = RoutingDataManager.GetChannelAccount(requestor, out bool isBot);

            if (requestorChannelAccount == null)
            {
                throw new ArgumentNullException("The channel account of the requestor is null");
            }

            string requestorChannelAccountName = string.IsNullOrEmpty(requestorChannelAccount.Name)
                ? StringConstants.NoUserNamePlaceholder : requestorChannelAccount.Name;

            string requestorChannelId = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(requestor.ChannelId);
            string requestorChannelAccountId = requestorChannelAccount.Id;

            string acceptCommand =
                new Command(Commands.AcceptRequest, new string[] { requestorChannelAccountId }, botName).ToString();

            string rejectCommand =
                new Command(Commands.RejectRequest, new string[] { requestorChannelAccountId }, botName).ToString();

            HeroCard card = new HeroCard()
            {
                Title = Strings.ConnectionRequestTitle,
                Subtitle = string.Format(Strings.RequestorDetailsTitle, requestorChannelAccountName, requestorChannelId),
                Text = string.Format(Strings.AcceptRejectConnectionHint, acceptCommand, rejectCommand),

                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = Strings.AcceptButtonTitle,
                        Type = ActionTypes.ImBack,
                        Value = acceptCommand
                    },
                    new CardAction()
                    {
                        Title = Strings.RejectButtonTitle,
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
        /// <param name="connectionRequests">The connection requests.</param>
        /// <param name="botName">The name of the bot (optional).</param>
        /// <returns>A list of request cards as attachments.</returns>
        public static IList<Attachment> CreateMultipleRequestCards(IList<ConnectionRequest> connectionRequests, string botName)
        {
            IList<Attachment> attachments = new List<Attachment>();

            foreach (ConnectionRequest connectionRequest in connectionRequests)
            {
                if (RoutingDataManager.GetChannelAccount(connectionRequest.Requestor, out bool isBot) != null)
                {
                    attachments.Add(CreateRequestCard(connectionRequest.Requestor, botName).ToAttachment());
                }
            }

            return attachments;
        }

        /// <summary>
        /// Creates a card for accepting/rejecting multiple requests.
        /// </summary>
        /// <param name="connectionRequests">The connection requests.</param>
        /// <param name="doAccept">If true, will create an accept card. If false, will create a reject card.</param>
        /// <param name="botName">The name of the bot (optional).</param>
        /// <returns>The newly created card.</returns>
        public static HeroCard CreateAcceptOrRejectCardForMultipleRequests(
            IList<ConnectionRequest> connectionRequests, bool doAccept, string botName)
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

            string command = null;
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

                command = new Command(
                    (doAccept ? Commands.AcceptRequest : Commands.RejectRequest),
                    new string[] { requestorChannelAccountId }, botName).ToString();

                card.Buttons.Add(new CardAction()
                {
                    Title = string.Format(
                        Strings.RequestorDetailsItem,
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