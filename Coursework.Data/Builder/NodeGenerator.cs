﻿using System.Collections.Generic;
using System.Linq;
using Coursework.Data.Entities;
using Coursework.Data.MessageServices;

namespace Coursework.Data.Builder
{
    public static class NodeGenerator
    {
        private static int _currentId;

        public static Node[] GenerateNodes(int count)
        {
            var nodes = Enumerable
                .Range(_currentId, count)
                .Select(CreateNode)
                .ToArray();

            _currentId += count;

            return nodes;
        }

        public static void ResetAccumulator()
        {
            _currentId = 0;
        }

        private static Node CreateNode(int id)
        {
            return new Node
            {
                Id = (uint)id,
                MessageQueue = new MessageQueueHandler(),
                LinkedNodesId = new SortedSet<uint>()
            };
        }
    }
}