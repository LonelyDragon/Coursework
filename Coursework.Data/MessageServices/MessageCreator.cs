﻿using System;
using System.Collections.Generic;
using System.Linq;
using Coursework.Data.Constants;
using Coursework.Data.Entities;
using Coursework.Data.NetworkData;

namespace Coursework.Data.MessageServices
{
    public class MessageCreator : IMessageCreator
    {
        protected readonly INetworkHandler Network;
        protected readonly IMessageRouter MessageRouter;

        public MessageCreator(INetworkHandler network, IMessageRouter messageRouter)
        {
            Network = network;
            MessageRouter = messageRouter;
        }

        public void UpdateTables()
        {
            var centralMachine = Network.Nodes
                .FirstOrDefault(n => n.NodeType == NodeType.CentralMachine);

            if (centralMachine != null && centralMachine.IsActive)
            {
                var networkMatrises = CreateNetworkMatrises();

                centralMachine.NetworkMatrix = networkMatrises[centralMachine.Id];
                centralMachine.IsTableUpdated = true;

                CreateInitializeMessages(centralMachine, networkMatrises);
            }
        }

        public virtual Message[] CreateMessages(MessageInitializer messageInitializer)
        {
            var route = MessageRouter.GetRoute(messageInitializer.SenderId,
                messageInitializer.ReceiverId);

            if (route == null)
            {
                return null;
            }

            var message = CreateMessage(messageInitializer, route);

            return new[] { message };
        }

        public void AddInQueue(Message[] messages, uint nodeId)
        {
            foreach (var message in messages)
            {
                Network.AddInQueue(message, nodeId);
            }

            var node = Network.GetNodeById(nodeId);
            node.NetworkMatrix = MessageRouter.CountPriceMatrix(nodeId, nodeId, node.NetworkMatrix);
        }

        public void RemoveFromQueue(Message[] messages, uint nodeId)
        {
            foreach (var message in messages)
            {
                Network.RemoveFromQueue(message, nodeId);
            }

            var node = Network.GetNodeById(nodeId);
            node.NetworkMatrix = MessageRouter.CountPriceMatrix(nodeId, nodeId, node.NetworkMatrix);
        }

        private void CreateInitializeMessages(Node centralMachine, Dictionary<uint, NetworkMatrix> networkMatrises)
        {
            foreach (var linkedNodeId in centralMachine.LinkedNodesId)
            {
                var linkedNode = Network.GetNodeById(linkedNodeId);

                if (!linkedNode.IsActive)
                {
                    continue;
                }

                var messageInitializer = new MessageInitializer
                {
                    Size = AllConstants.InitializeMessageSize,
                    MessageType = MessageType.MatrixUpdateMessage,
                    ReceiverId = linkedNodeId,
                    SenderId = centralMachine.Id,
                    Data = networkMatrises
                };

                var messages = CreateMessages(messageInitializer);

                if (messages != null)
                {
                    foreach (var message in messages)
                    {
                        var messageQueue = centralMachine.MessageQueueHandlers
                            .First(m => m.ChannelId == message.Route[0].Id);

                        messageQueue.AddMessageInStart(message);
                    }
                }
            }
        }

        private Dictionary<uint, NetworkMatrix> CreateNetworkMatrises()
        {
            var networkMatrises = new Dictionary<uint, NetworkMatrix>();

            foreach (var node in Network.Nodes)
            {
                var networkMatrix = MessageRouter.CountPriceMatrix(node.Id, null);

                node.IsTableUpdated = false;
                networkMatrises[node.Id] = networkMatrix;
            }

            return networkMatrises;
        }

        private Message CreateMessage(MessageInitializer messageInitializer, Channel[] route)
        {
            var message = new Message
            {
                MessageType = messageInitializer.MessageType,
                ReceiverId = messageInitializer.ReceiverId,
                LastTransferNodeId = messageInitializer.SenderId,
                Route = route,
                SenderId = messageInitializer.SenderId,
                Data = messageInitializer.Data,
                DataSize = GetDataSize(messageInitializer),
                ServiceSize = GetServiceSize(messageInitializer),
                ParentId = Guid.NewGuid(),
                SendAttempts = 0
            };

            if (message.MessageType == MessageType.General)
            {
                var positiveReceiveResponse = CreatePositiveReceiveResponse(messageInitializer, message);

                message.Data = new[] {positiveReceiveResponse};
            }

            return message;
        }

        private static Message CreatePositiveReceiveResponse(MessageInitializer messageInitializer, Message message)
        {
            var reversedRoute = message.Route
                .ToArray()
                .Reverse();

            var positiveReceiveResponse = new Message
            {
                MessageType = MessageType.PositiveReceiveResponse,
                Data = null,
                ReceiverId = messageInitializer.SenderId,
                SenderId = messageInitializer.ReceiverId,
                Route = reversedRoute.ToArray(),
                ParentId = message.ParentId,
                ServiceSize = AllConstants.ReceiveResponseMessageSize,
                LastTransferNodeId = messageInitializer.ReceiverId,
                SendAttempts = 0,
            };
            return positiveReceiveResponse;
        }

        private int GetDataSize(MessageInitializer messageInitializer)
        {
            if (messageInitializer.MessageType == MessageType.General)
            {
                return messageInitializer.Size;
            }

            return 0;
        }

        private int GetServiceSize(MessageInitializer messageInitializer)
        {
            if (messageInitializer.MessageType == MessageType.MatrixUpdateMessage
                || messageInitializer.MessageType == MessageType.NegativeSendingResponse
                || messageInitializer.MessageType == MessageType.PositiveSendingResponse
                || messageInitializer.MessageType == MessageType.SendingRequest
                || messageInitializer.MessageType == MessageType.PositiveReceiveResponse)
            {
                return messageInitializer.Size + AllConstants.ServicePartSize;
            }

            return AllConstants.ServicePartSize;
        }
    }
}
