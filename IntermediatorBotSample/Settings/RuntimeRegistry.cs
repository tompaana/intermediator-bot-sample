using Microsoft.Extensions.DependencyInjection;
using StructureMap;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.DataStore.Local;

namespace IntermediatorBotSample.Settings
{
    public class RuntimeRegistry : Registry
    {
        public RuntimeRegistry(IServiceCollection services)
        {
            Scan(x => {
                x.AssemblyContainingType(typeof(Startup));
                x.AssembliesAndExecutablesFromApplicationBaseDirectory();
                x.WithDefaultConventions();
            });
            For<IRoutingDataManager>().Use<LocalRoutingDataManager>();

            this.Populate(services);
        }       
    }
}
