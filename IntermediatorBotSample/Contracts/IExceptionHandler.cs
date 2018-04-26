using System;
using System.Threading.Tasks;

namespace IntermediatorBotSample.Contracts
{
    public interface IExceptionHandler
    {
        /// <summary>
        /// Executes an asynchronous method and applies a general catch handler
        /// </summary>
        /// <param name="unsafeFunction">the potentially unsafe function to execute</param>        
        Task ExecuteAsync(Func<Task> unsafeFunction);


        /// <summary>
        /// Executes an asynchronous method that returns an object of type TContract and applies a general catch handler
        /// </summary>
        /// <param name="unsafeFunction">the potentially unsafe function to execute</param>        
        /// <returns>The result of the unsafe function or default value for the return type</returns>
        Task<TContract> GetAsync<TContract>(Func<Task<TContract>> unsafeFunction);
    }
}
