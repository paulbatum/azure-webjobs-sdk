﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.ServiceBus
{
    /// <summary>
    /// Setup an 'output' binding to an EventHub. This can be any output type compatible with an IAsyncCollector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class EventHubAttribute : Attribute
    {
        /// <summary>
        /// The name of the event hub. This is resolved againt the <see cref="EventHubConfiguration"/>
        /// </summary>
        public string EventHubName { get; private set; }

        /// <summary>
        /// Initialize a new instance of hte <see cref="EventHubAttribute"/>
        /// </summary>
        /// <param name="eventHubName">Name of the event hub as resolved agaisnt the <see cref="EventHubConfiguration"/> </param>
        public EventHubAttribute(string eventHubName)
        {
            this.EventHubName = eventHubName;
        }
    }    
}