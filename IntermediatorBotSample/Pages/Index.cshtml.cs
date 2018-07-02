using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace IntermediatorBotSample.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string BotEndpointPath
        {
            get
            {
                return $"{Configuration["BotBasePath"]}{Configuration["BotMessagesPath"]}";
            }
        }

        [BindProperty]
        public string BotAppId
        {
            get
            {
#if DEBUG
                return Configuration["MicrosoftAppId"];
#else
                return "None of your business";
#endif
            }
        }

        [BindProperty]
        public string BotAppPassword
        {
            get
            {
#if DEBUG
                return Configuration["MicrosoftAppPassword"];
#else
                return "Also none of your business";
#endif
            }
        }

        private IConfiguration Configuration
        {
            get;
        }

        public IndexModel(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void OnGet()
        {
        }
    }
}
