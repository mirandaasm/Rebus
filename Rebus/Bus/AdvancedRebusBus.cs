﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Bus.Advanced;
using Rebus.DataBus;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Time;
using Rebus.Transport;
// ReSharper disable ArgumentsStyleLiteral

namespace Rebus.Bus
{
    /// <summary>
    /// The implementations of the advanced APIs are private classes inside <see cref="RebusBus"/> so that they can access private functions and stuff
    /// </summary>
    public partial class RebusBus
    {
        class AdvancedApi : IAdvancedApi
        {
            readonly RebusBus _rebusBus;

            public AdvancedApi(RebusBus rebusBus)
            {
                _rebusBus = rebusBus;
            }

            public IWorkersApi Workers => new WorkersApi(_rebusBus);

            public ITopicsApi Topics => new TopicsApi(_rebusBus);

            public IRoutingApi Routing => new RoutingApi(_rebusBus);

            public ITransportMessageApi TransportMessage => new TransportMessageApi(_rebusBus);

            public IDataBus DataBus => _rebusBus._dataBus;

            public ISyncBus SyncBus => new SyncApi(_rebusBus);
        }

        class TransportMessageApi : ITransportMessageApi
        {
            readonly RebusBus _rebusBus;

            public TransportMessageApi(RebusBus rebusBus)
            {
                _rebusBus = rebusBus;
            }

            public async Task Forward(string destinationAddress, Dictionary<string, string> optionalAdditionalHeaders = null)
            {
                var transportMessage = GetCloneOfCurrentTransportMessage(optionalAdditionalHeaders);

                await _rebusBus.SendTransportMessage(destinationAddress, transportMessage);
            }

            public async Task Defer(TimeSpan delay, Dictionary<string, string> optionalAdditionalHeaders = null)
            {
                var transportMessage = GetCloneOfCurrentTransportMessage(optionalAdditionalHeaders);
                var timeoutManagerAddress = _rebusBus.GetTimeoutManagerAddress();

                transportMessage.SetDeferHeaders(RebusTime.Now + delay, _rebusBus._transport.Address);

                await _rebusBus.SendTransportMessage(timeoutManagerAddress, transportMessage);
            }

            TransportMessage GetCloneOfCurrentTransportMessage(Dictionary<string, string> optionalAdditionalHeaders)
            {
                var transactionContext = _rebusBus.GetCurrentTransactionContext(mustBelongToThisBus: false);

                if (transactionContext == null)
                {
                    throw new InvalidOperationException(
                        "Attempted to perform operation on the current transport message, but there was no transaction context and therefore no 'current transport message' to do anything with! This call must be made from within a message handler... if you're actually doing that, you're probably experiencing this error because you're executing this operation on an independent thread somehow...");
                }

                var originalTransportMessage = transactionContext
                    .GetOrThrow<IncomingStepContext>(StepContext.StepContextKey)
                    .Load<OriginalTransportMessage>();

                if (originalTransportMessage == null)
                {
                    throw new InvalidOperationException(
                        "Attempted to perform operation on the current transport message, but no transport message could be found in the context.... this is odd because the entire receive pipeline will only get called when there is a transport message, so maybe it has somehow been removed?");
                }

                var currentTransportMessage = originalTransportMessage.TransportMessage;

                var headers = optionalAdditionalHeaders != null
                    ? currentTransportMessage.Headers.MergedWith(optionalAdditionalHeaders)
                    : currentTransportMessage.Headers.Clone();

                var body = currentTransportMessage.Body;
                var clone = new TransportMessage(headers, body);

                return clone;
            }
        }

        class SyncApi : ISyncBus
        {
            readonly RebusBus _rebusBus;

            public SyncApi(RebusBus rebusBus)
            {
                _rebusBus = rebusBus;
            }

            public void SendLocal(object commandMessage, Dictionary<string, string> optionalHeaders = null)
            {
                AsyncHelpers.RunSync(() => _rebusBus.SendLocal(commandMessage, optionalHeaders));
            }

            public void Send(object commandMessage, Dictionary<string, string> optionalHeaders = null)
            {
                AsyncHelpers.RunSync(() => _rebusBus.Send(commandMessage, optionalHeaders));
            }

            public void Reply(object replyMessage, Dictionary<string, string> optionalHeaders = null)
            {
                AsyncHelpers.RunSync(() => _rebusBus.Reply(replyMessage, optionalHeaders));
            }

            public void Defer(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null)
            {
                AsyncHelpers.RunSync(() => _rebusBus.Defer(delay, message, optionalHeaders));
            }

            public void Subscribe<TEvent>()
            {
                AsyncHelpers.RunSync(() => _rebusBus.Subscribe<TEvent>());
            }

            public void Subscribe(Type eventType)
            {
                AsyncHelpers.RunSync(() => _rebusBus.Subscribe(eventType));
            }

            public void Unsubscribe<TEvent>()
            {
                AsyncHelpers.RunSync(() => _rebusBus.Unsubscribe<TEvent>());
            }

            public void Unsubscribe(Type eventType)
            {
                AsyncHelpers.RunSync(() => _rebusBus.Unsubscribe(eventType));
            }

            public void Publish(object eventMessage, Dictionary<string, string> optionalHeaders = null)
            {
                AsyncHelpers.RunSync(() => _rebusBus.Publish(eventMessage, optionalHeaders));
            }
        }

        class RoutingApi : IRoutingApi
        {
            readonly RebusBus _rebusBus;

            public RoutingApi(RebusBus rebusBus)
            {
                _rebusBus = rebusBus;
            }

            public Task Send(string destinationAddress, object explicitlyRoutedMessage, Dictionary<string, string> optionalHeaders = null)
            {
                var logicalMessage = CreateMessage(explicitlyRoutedMessage, Operation.Send, optionalHeaders);

                return _rebusBus.InnerSend(new[] { destinationAddress }, logicalMessage);
            }
        }

        class WorkersApi : IWorkersApi
        {
            readonly RebusBus _rebusBus;

            public WorkersApi(RebusBus rebusBus)
            {
                _rebusBus = rebusBus;
            }

            public int Count => _rebusBus.GetNumberOfWorkers();

            public void SetNumberOfWorkers(int numberOfWorkers)
            {
                _rebusBus.SetNumberOfWorkers(numberOfWorkers);
            }
        }

        class TopicsApi : ITopicsApi
        {
            readonly RebusBus _rebusBus;

            public TopicsApi(RebusBus rebusBus)
            {
                _rebusBus = rebusBus;
            }

            public Task Publish(string topic, object eventMessage, Dictionary<string, string> optionalHeaders = null)
            {
                return _rebusBus.InnerPublish(topic, eventMessage, optionalHeaders);
            }

            public Task Subscribe(string topic)
            {
                return _rebusBus.InnerSubscribe(topic);
            }

            public Task Unsubscribe(string topic)
            {
                return _rebusBus.InnerUnsubscribe(topic);
            }
        }
    }
}