﻿using System.Linq;
using Coursework.Data.Constants;
using Coursework.Data.Entities;
using Coursework.Data.NetworkData;

namespace Coursework.Data.MessageServices
{
    public class RequestMessageCreator : PackageMessageCreator
    {
        public RequestMessageCreator(INetworkHandler network, IMessageRouter messageRouter)
            : base(network, messageRouter)
        {
        }

        public override Message[] CreateMessages(MessageInitializer messageInitializer)
        {
            var messages = base.CreateMessages(messageInitializer);

            if (messages != null)
            {
                foreach (var message in messages)
                {
                    message.Route = messages[0].Route.ToArray();
                }

                var request = CreateRequestMessage(messages);

                return request;
            }

            return null;
        }

        private Message[] CreateRequestMessage(Message[] messages)
        {
            var message = CreateMessage(messages);

            return new[] { message };
        }

        private Message CreateMessage(Message[] currentMessages)
        {
            var firstMessage = currentMessages.First();

            return new Message
            {
                Data = currentMessages,
                LastTransferNodeId = firstMessage.LastTransferNodeId,
                MessageType = MessageType.SendingRequest,
                ParentId = firstMessage.ParentId,
                ReceiverId = firstMessage.ReceiverId,
                SendAttempts = 0,
                SenderId = firstMessage.SenderId,
                DataSize = 0,
                ServiceSize = AllConstants.SendingRequestMessageSize,
                Route = firstMessage.Route,
                NumberInPackage = 0
            };
        }
    }
}
