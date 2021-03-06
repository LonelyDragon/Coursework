﻿using System;
using System.Collections.Generic;
using System.Linq;
using Coursework.Data.Entities;
using Coursework.Data.Exceptions;
using Coursework.Data.MessageServices;
using Coursework.Data.NetworkData;
using Moq;
using NUnit.Framework;

namespace Coursework.Tests
{
    [TestFixture]
    public class MessageRepositoryTests
    {
        private Mock<INetworkHandler> _networkMock;
        private IMessageRepository _messageRepository;
        private Node[] _nodes;
        private Channel[] _channels;
        private Node FirstNode => _nodes.First();

        [SetUp]
        public void Setup()
        {
            _networkMock = new Mock<INetworkHandler>();

            _messageRepository = new MessageRepository(_networkMock.Object);

            _nodes = new[]
            {
                new Node
                {
                    Id = 0,
                    MessageQueueHandlers = new List<MessageQueueHandler>
                    {
                        new MessageQueueHandler(Guid.Empty)
                    },
                    ReceivedMessages = new List<Message>
                    {
                        new Message
                        {
                            LastTransferNodeId = 1,
                            ReceiverId = 1,
                            IsReceived = true,
                            Route = new Channel[0]
                        },
                        new Message
                        {
                            LastTransferNodeId = 1,
                            ReceiverId = 1,
                            IsReceived = true,
                            Route = new Channel[0]
                        },
                        new Message
                        {
                            LastTransferNodeId = 1,
                            ReceiverId = 1,
                            IsReceived = true,
                            Route = new Channel[0]
                        },
                    },
                    CanceledMessages = new List<Message>
                    {
                        new Message
                        {
                            IsCanceled = true,
                            Route = new Channel[0]
                        },
                        new Message
                        {
                            IsCanceled = true,
                            Route = new Channel[0]
                        }
                    }
                }
            };

            _channels = new[]
            {
                new Channel
                {
                    Id = Guid.Empty,
                    FirstNodeId = 0,
                    SecondNodeId = 1,
                    FirstMessage = new Message(),
                    SecondMessage = null,
                    Capacity = 2
                }
            };

            _networkMock.Setup(n => n.Nodes)
                .Returns(_nodes);

            _networkMock.Setup(n => n.Channels)
                .Returns(_channels);

            _networkMock.Setup(n => n.GetNodeById(FirstNode.Id))
                .Returns(FirstNode);

            FirstNode.MessageQueueHandlers.First().AppendMessage(new Message { Route = new Channel[0] });
            FirstNode.MessageQueueHandlers.First().AppendMessage(new Message { Route = new Channel[0] });
            FirstNode.MessageQueueHandlers.First().AppendMessage(new Message { Route = new Channel[0] });
            FirstNode.MessageQueueHandlers.First().AppendMessage(new Message { Route = new Channel[0] });
        }

        [Test]
        public void GetAllMessagesShouldReturnAllMessagesInNetwork()
        {
            // Arrange
            var messageCountInNodes = _nodes.SelectMany(n => n.MessageQueueHandlers)
                .SelectMany(m => m.Messages)
                .Count();

            var messageCountInChannels = _channels.Select(c => c.FirstMessage).Count(m => m != null)
                + _channels.Select(c => c.SecondMessage).Count(m => m != null);

            var receivedMessagesCount = _nodes.SelectMany(n => n.ReceivedMessages)
                .Count();

            var canceledMessagesCount = _nodes.SelectMany(n => n.CanceledMessages)
                .Count();

            // Act
            var result = _messageRepository.GetAllMessages();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(messageCountInChannels + messageCountInNodes
                + receivedMessagesCount + canceledMessagesCount));
        }

        [Test]
        public void GetAllMessagesShouldReturnAllMessagesInSpecifiedNode()
        {
            // Arrange
            var messageCountInNode = FirstNode.MessageQueueHandlers
               .SelectMany(m => m.Messages)
               .Count();

            var receivedMessagesCount = FirstNode.ReceivedMessages.Count;

            var canceledMessagesCount = FirstNode.CanceledMessages.Count;

            // Act
            var result = _messageRepository.GetAllMessages(FirstNode.Id);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(messageCountInNode + receivedMessagesCount + canceledMessagesCount));
        }

        [Test]
        public void GetAllMessagesShouldThrowExceptionIfNodeNotExists()
        {
            // Arrange
            // Act
            TestDelegate testDelegate = () => _messageRepository.GetAllMessages(uint.MaxValue);

            // Assert
            Assert.That(testDelegate, Throws.TypeOf(typeof(NodeException)));
        }

        [Test]
        public void GetAllMessagesShouldReturnOnlySpecifiedMessages()
        {
            // Arrange
            var receivedMessagesCount = _nodes.SelectMany(n => n.ReceivedMessages)
                .Count(m => m.IsReceived);

            // Act
            var result = _messageRepository.GetAllMessages(messageFiltrationMode:
                MessageFiltrationMode.ReceivedMessagesOnly);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(receivedMessagesCount));
        }

        [Test]
        public void GetAllMessagesShouldReturnOnlySpecifiedMessagesFromSpecifiedNode()
        {
            // Arrange
            // Act
            var result = _messageRepository.GetAllMessages(FirstNode.Id, MessageFiltrationMode.ReceivedMessagesOnly);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(FirstNode.ReceivedMessages.Count));
        }

        [Test]
        public void GetAllMessagesShouldReturnSpecifiedMessagesWithCombinationOfFiltrationModes()
        {
            // Arrange
            var messageCountInNodes = _nodes.SelectMany(n => n.MessageQueueHandlers)
                .SelectMany(m => m.Messages)
                .Count();

            var messageCountInChannels = _channels.Select(c => c.FirstMessage).Count(m => m != null)
                + _channels.Select(c => c.SecondMessage).Count(m => m != null);

            // Act
            var result = _messageRepository.GetAllMessages(messageFiltrationMode: MessageFiltrationMode.ActiveMessages);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(messageCountInChannels + messageCountInNodes));
        }

        [Test]
        public void GetAllMessagesShouldReturnCanceledMessagesOnly()
        {
            // Arrange
            var canceledMessages = _nodes.SelectMany(n => n.CanceledMessages)
                .Count(m => m.IsCanceled);

            // Act
            var result = _messageRepository.GetAllMessages(messageFiltrationMode:
                MessageFiltrationMode.CanceledMessagesOnly);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(canceledMessages));
        }
    }
}
