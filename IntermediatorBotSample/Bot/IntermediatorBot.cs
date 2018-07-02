using IntermediatorBotSample.CommandHandling;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IntermediatorBotSample.Bot
{
    public class IntermediatorBot : IBot
    {
        private const string SampleUrl = "https://github.com/tompaana/intermediator-bot-sample";

        public async Task OnTurn(ITurnContext context)
        {
            Command showOptionsCommand = new Command(Commands.ShowOptions);

            HeroCard heroCard = new HeroCard()
            {
                Title = "Hello!",
                Subtitle = "I am Intermediator Bot",
                Text = $"My purpose is to serve as a sample on how to implement the human hand-off. Click/tap the button below or type \"{new Command(Commands.ShowOptions).ToString()}\" to see all possible commands. To learn more visit <a href=\"{SampleUrl}\">{SampleUrl}</a>.",
                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = "Show options",
                        Value = showOptionsCommand.ToString(),
                        Type = ActionTypes.ImBack
                    }
                }
            };

            Activity replyActivity = context.Activity.CreateReply();
            replyActivity.Attachments = new List<Attachment>() { heroCard.ToAttachment() };
            await context.SendActivity(replyActivity);
        }
    }
}
