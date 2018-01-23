using System;
using System.Collections.Generic;
using System.Linq;

namespace IntermediatorBotSample.CommandHandling
{
    /// <summary>
    /// Constants for commands.
    /// </summary>
    public struct Commands
    {
        public const string CommandKeyword = "command"; // Used if the channel does not support mentions

        public const string CommandListOptions = "options";
        public const string CommandAddAggregationChannel = "watch";
        public const string CommandRemoveAggregationChannel = "unwatch";
        public const string CommandAcceptRequest = "accept";
        public const string CommandRejectRequest = "reject";
        public const string CommandDisconnect = "disconnect";

        public const string CommandParameterAll = "*";

#if DEBUG // Commands for debugging
        public const string CommandDeleteAllRoutingData = "reset";
        public const string CommandList = "list";

        public const string CommandParameterParties = "parties";
        public const string CommandParameterRequests = "requests";
        public const string CommandParameterConnections = "connections";
        public const string CommandParameterResults = "results";
#endif

        public const string CommandRequestConnection = "human"; // For "customers"
    }

    /// <summary>
    /// Command representation.
    /// </summary>
    public class Command
    {
        /// <summary>
        /// The actual command such as "watch" or "unwatch".
        /// </summary>
        public string BaseCommand
        {
            get;
            protected set;
        }

        /// <summary>
        /// The command parameters.
        /// </summary>
        public IList<string> Parameters
        {
            get;
            protected set;
        }

        public Command(string baseCommand, string[] parameters = null)
        {
            if (string.IsNullOrEmpty(baseCommand))
            {
                throw new ArgumentNullException("The base command cannot be null");
            }

            BaseCommand = baseCommand;

            if (parameters != null)
            {
                Parameters = parameters.ToList();
            }
            else
            {
                Parameters = new List<string>();
            }
        }

        /// <summary>
        /// Resolves the full command string.
        /// </summary>
        /// <param name="botName">The bot name (handle). If null or empty, the basic commmand keyword is used.</param>
        /// <param name="command">The command.</param>
        /// <returns>The generated full command string.</returns>
        public static string ResolveFullCommand(string botName, Command command)
        {
            if (command == null)
            {
                throw new ArgumentNullException("The command cannot be null");
            }

            string fullCommand = string.Empty;

            if (string.IsNullOrEmpty(botName))
            {
                fullCommand = $"{Commands.CommandKeyword} {command.ToString()}";
            }
            else
            {
                fullCommand = $"@{botName} {command.ToString()}";
            }

            return fullCommand;
        }

        /// <summary>
        /// Resolves the full command string.
        /// </summary>
        /// <param name="botName">The bot name (handle). If null or empty, the basic commmand keyword is used.</param>
        /// <param name="baseCommand">The base command.</param>
        /// <param name="parameters">The command parameters (if any).</param>
        /// <returns>The generated full command string.</returns>
        public static string ResolveFullCommand(string botName, string baseCommand, string[] parameters = null)
        {
            if (string.IsNullOrEmpty(baseCommand))
            {
                throw new ArgumentNullException("The base command is missing");
            }

            return ResolveFullCommand(botName, new Command(baseCommand, parameters));
        }

        public override string ToString()
        {
            string commandAsString = string.Empty;

            if (!string.IsNullOrEmpty(BaseCommand))
            {
                commandAsString = BaseCommand;

                if (Parameters != null && Parameters.Count > 0)
                {
                    foreach (string parameter in Parameters)
                    {
                        commandAsString += $" {parameter}";
                    }
                }
            }

            return commandAsString;
        }
    }
}
