using Microsoft.Bot;
using Microsoft.Bot.Builder;
using System.Threading.Tasks;

namespace IntermediatorBotSample
{
    public class IntermediatorBot : IBot
    {
        public async Task OnTurn(ITurnContext context)
        {
            await context.SendActivity($"Hello world.");
        }
    }
}
