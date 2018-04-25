using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Threading.Tasks;

namespace IntermediatorBotSample
{
    public class IntermediatorBot : IBot
    {
        public async Task OnTurn(ITurnContext context)
        {
            if (context.Activity.Type is ActivityTypes.Message)
            {
                await context.SendActivity($"Hello world.");
            }
        }
    }
}
