﻿using System;
using Coursework.Data.Entities;
using Coursework.Data.NetworkData;

namespace Coursework.Data.MessageServices
{
    public class NegativeResponseCreator : ResponseCreator
    {
        public NegativeResponseCreator(INetworkHandler network, IMessageRouter messageRouter) 
            : base(network, messageRouter)
        {
        }

        protected override void ThrowExceptionIfMessageTypeIsNotCorrectResponse(MessageType responseType)
        {
            if (responseType != MessageType.NegativeSendingResponse)
            {
                throw new ArgumentException("responseType");
            }
        }
    }
}
