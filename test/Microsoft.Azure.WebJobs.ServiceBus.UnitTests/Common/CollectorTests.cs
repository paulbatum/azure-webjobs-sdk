﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.TestCommon;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.WebJobs.ServiceBus.UnitTests
{
    // Test IAsyncCollector binding permutations using the FakeQueue mocks. 
    public class CollectorTests
    {
        // dummy Poco to use with test. 
        public class Payload
        {
            public int val1 { get; set; }
        }

        // Various flavors that all bind down to an IAsyncCollector 
        public class Functions
        {
            public static void SendDontQueue([FakeQueue] out Payload output)
            {
                output = null;
            }
            public static void SendArrayNull([FakeQueue] out string[] outputs)
            {
                outputs = null; // don't queue
            }

            public static void SendArrayLen0([FakeQueue] out string[] outputs)
            {
                outputs = new string[0]; // don't queue
            }

            public static void SendOnePoco([FakeQueue] out Payload output)
            {
                output = new Payload { val1 = 123 };
            }

            public static void SendOneNative([FakeQueue] out FakeQueueData output)
            {
                output = new FakeQueueData
                {
                    ExtraPropertery = "extra",
                    Message = "message"
                };
            }

            public static void SendOneString([FakeQueue] out string output)
            {
                output = "stringvalue";
            }

            public static void SendArrayString([FakeQueue] out string[] outputs)
            {
                outputs = new string[] {
                    "first", "second"
                };
            }

            public static void SendSyncCollectorBytes([FakeQueue] ICollector<byte[]> collector)
            {
                byte[] first = Encoding.UTF8.GetBytes("first");
                collector.Add(first);

                byte[] second = Encoding.UTF8.GetBytes("second");
                collector.Add(second);
            }

            public static void SendSyncCollectorString([FakeQueue] ICollector<string> collector)
            {
                collector.Add("first");
                collector.Add("second");
            }

            public static void SendAsyncCollectorString([FakeQueue] IAsyncCollector<string> collector)
            {
                collector.AddAsync("first").Wait();
                collector.AddAsync("second").Wait();
            }

            public static void SendCollectorNative([FakeQueue] ICollector<FakeQueueData> collector)
            {
                collector.Add(new FakeQueueData { Message = "first" });
                collector.Add(new FakeQueueData { Message = "second" });
            }

            public static void SendCollectorPoco([FakeQueue] ICollector<Payload> collector)
            {
                collector.Add(new Payload { val1 = 100 });
                collector.Add(new Payload { val1 = 200 });
            }

            public static void SendArrayPoco([FakeQueue] out Payload[] array)
            {
                array = new Payload[] {
                    new Payload { val1 = 100 },
                    new Payload { val1 = 200 }
                };
            }
        }

        [Fact]
        public void Test()
        {
            JobHostConfiguration config = new JobHostConfiguration()
            {
                TypeLocator = new FakeTypeLocator(typeof(Functions))
            };

            FakeQueueClient client = new FakeQueueClient();
            IExtensionRegistry extensions = config.GetService<IExtensionRegistry>();
            extensions.RegisterExtension<IExtensionConfigProvider>(client);

            JobHost host = new JobHost(config);

            // Single items
            var p1 = InvokeJson<Payload>(host, client, "SendOnePoco");
            Assert.Equal(1, p1.Length);            
            Assert.Equal(123, p1[0].val1);

            var p2 = Invoke(host, client, "SendOneNative");
            Assert.Equal(1, p2.Length);
            Assert.Equal("message", p2[0].Message);
            Assert.Equal("extra", p2[0].ExtraPropertery);

            var p3 = Invoke(host, client, "SendOneString");
            Assert.Equal(1, p3.Length);
            Assert.Equal("stringvalue", p3[0].Message);

            foreach (string methodName in new string[] { "SendDontQueue", "SendArrayNull", "SendArrayLen0"})
            {
                var p6 = Invoke(host, client, methodName);
                Assert.Equal(0, p6.Length);
            }

            // batching 
            foreach(string methodName in new string[] {
                "SendSyncCollectorBytes", "SendArrayString", "SendSyncCollectorString", "SendAsyncCollectorString", "SendCollectorNative" })
            {
                var p4 = Invoke(host, client, methodName);
                Assert.Equal(2, p4.Length);
                Assert.Equal("first", p4[0].Message);
                Assert.Equal("second", p4[1].Message);
            }

            foreach(string methodName in new string[] { "SendCollectorPoco", "SendArrayPoco" })
            {
                var p5 = InvokeJson<Payload>(host, client, methodName);
                Assert.Equal(2, p5.Length);
                Assert.Equal(100, p5[0].val1);
                Assert.Equal(200, p5[1].val1);
            }            
        }

        static FakeQueueData[] Invoke(JobHost host, FakeQueueClient client, string name)
        {
            var method = typeof(Functions).GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            host.Call(method);

            var data = client._items.ToArray();
            client._items.Clear();
            return data;
        }

        static T[] InvokeJson<T>(JobHost host, FakeQueueClient client, string name)
        {
            var method = typeof(Functions).GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            host.Call(method);

            var data = client._items.ToArray();

            var obj = Array.ConvertAll(data, x => JsonConvert.DeserializeObject<T>(x.Message));
            client._items.Clear();
            return obj;
        }        
    }
}
