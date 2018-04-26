using Microsoft.Extensions.DependencyInjection;
using StructureMap;

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
            this.Populate(services);
        }
    }
}
