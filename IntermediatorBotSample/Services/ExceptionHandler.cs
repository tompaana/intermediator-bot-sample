using IntermediatorBotSample.Contracts;
using System;
using System.Threading.Tasks;

namespace IntermediatorBotSample.Services
{
    public class ExceptionHandler : IExceptionHandler
    {
        public async Task ExecuteAsync(Func<Task> unsafeFunction)
        {
            try
            {
                await unsafeFunction.Invoke();
            }
            catch
            {
                //TODO: General exception handling here
            }
        }


        public TContract Get<TContract>(Func<TContract> unsafeFunction)
        {
            try
            {
                return unsafeFunction.Invoke();
            }
            catch
            {
                //TODO: Exceptionhandling goes here
            }
            return default(TContract);
        }


        public async Task<TContract> GetAsync<TContract>(Func<Task<TContract>> unsafeFunction)
        {
            try
            {
                return await unsafeFunction.Invoke();
            }
            catch
            {
                //TODO: General exception handling here
            }
            return default(TContract);
        }
    }
}
