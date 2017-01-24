using Microsoft.Bot.Connector;
using System;

// This file contains specialized EventArgs implementations for MessageRouterManager class.
namespace MessageRouting
{
    public enum ChangeTypes
    {
        Initiated, // Not established (added) yet, but the wheels are set in motion, request sent etc.
        Added,
        Removed
    }

    public class EngagementChangedEventArgs : EventArgs
    {
        public ChangeTypes ChangeType
        {
            get;
            set;
        }

        public Party ConversationOwnerParty
        {
            get;
            set;
        }

        public Party ConversationClientParty
        {
            get;
            set;
        }
    }

    public class MessageRouterFailureEventArgs : EventArgs
    {
        /// <summary>
        /// Activity instance associated with the failure.
        /// </summary>
        public Activity Activity
        {
            get;
            set;
        }

        public string ErrorMessage
        {
            get;
            set;
        }

        public MessageRouterFailureEventArgs()
        {
            ErrorMessage = string.Empty;
        }
    }
}