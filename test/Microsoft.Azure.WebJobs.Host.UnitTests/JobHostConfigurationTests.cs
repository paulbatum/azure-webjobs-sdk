﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Indexers;
using Microsoft.Azure.WebJobs.Host.TestCommon;
using Xunit;

namespace Microsoft.Azure.WebJobs.Host.UnitTests
{
    public class JobHostConfigurationTests
    {
        [Fact]
        public void ConstructorDefaults()
        {
            JobHostConfiguration config = new JobHostConfiguration();

            Assert.NotNull(config.Singleton);

            Assert.NotNull(config.Tracing);
            Assert.Equal(TraceLevel.Info, config.Tracing.ConsoleLevel);
            Assert.Equal(0, config.Tracing.Tracers.Count);
            Assert.Null(config.FunctionTimeout);

            StorageClientFactory clientFactory = config.GetService<StorageClientFactory>();
            Assert.NotNull(clientFactory);
        }

        [Fact]
        public void HostId_IfNull_DoesNotThrow()
        {
            // Arrange
            JobHostConfiguration configuration = new JobHostConfiguration();
            string hostId = null;

            // Act & Assert
            configuration.HostId = hostId;
        }

        [Fact]
        public void HostId_IfValid_DoesNotThrow()
        {
            // Arrange
            JobHostConfiguration configuration = new JobHostConfiguration();
            string hostId = "abc";

            // Act & Assert
            configuration.HostId = hostId;
        }

        [Fact]
        public void HostId_IfMinimumLength_DoesNotThrow()
        {
            // Arrange
            JobHostConfiguration configuration = new JobHostConfiguration();
            string hostId = "a";

            // Act & Assert
            configuration.HostId = hostId;
        }

        [Fact]
        public void HostId_IfMaximumLength_DoesNotThrow()
        {
            // Arrange
            JobHostConfiguration configuration = new JobHostConfiguration();
            const int maximumValidCharacters = 32;
            string hostId = new string('a', maximumValidCharacters);

            // Act & Assert
            configuration.HostId = hostId;
        }

        [Fact]
        public void HostId_IfContainsEveryValidLetter_DoesNotThrow()
        {
            // Arrange
            JobHostConfiguration configuration = new JobHostConfiguration();
            string hostId = "abcdefghijklmnopqrstuvwxyz";

            // Act & Assert
            configuration.HostId = hostId;
        }

        [Fact]
        public void HostId_IfContainsEveryValidOtherCharacter_DoesNotThrow()
        {
            // Arrange
            JobHostConfiguration configuration = new JobHostConfiguration();
            string hostId = "0-123456789";

            // Act & Assert
            configuration.HostId = hostId;
        }

        [Fact]
        public void HostId_IfEmpty_Throws()
        {
            TestHostIdThrows(String.Empty);
        }

        [Fact]
        public void HostId_IfTooLong_Throws()
        {
            const int maximumValidCharacters = 32;
            string hostId = new string('a', maximumValidCharacters + 1);
            TestHostIdThrows(hostId);
        }

        [Fact]
        public void HostId_IfContainsInvalidCharacter_Throws()
        {
            // Uppercase character are not allowed.
            TestHostIdThrows("aBc");
        }

        [Fact]
        public void HostId_IfStartsWithDash_Throws()
        {
            TestHostIdThrows("-abc");
        }

        [Fact]
        public void HostId_IfEndsWithDash_Throws()
        {
            TestHostIdThrows("abc-");
        }

        [Fact]
        public void HostId_IfContainsConsecutiveDashes_Throws()
        {
            TestHostIdThrows("a--bc");
        }

        [Fact]
        public void JobActivator_IfNull_Throws()
        {
            JobHostConfiguration configuration = new JobHostConfiguration();

            ExceptionAssert.ThrowsArgumentNull(() => configuration.JobActivator = null, "value");
        }

        [Fact]
        public void FunctionTimeout_GetterSetter()
        {
            JobHostConfiguration configuration = new JobHostConfiguration();
            Assert.Null(configuration.FunctionTimeout);

            TimeSpan timeout = TimeSpan.FromHours(1);
            configuration.FunctionTimeout = timeout;
            Assert.Equal(timeout, configuration.FunctionTimeout);

            configuration.FunctionTimeout = null;
            Assert.Null(configuration.FunctionTimeout);
        }

        [Fact]
        public void GetService_IExtensionRegistry_ReturnsDefaultRegistry()
        {
            JobHostConfiguration configuration = new JobHostConfiguration();

            IExtensionRegistry extensionRegistry = configuration.GetService<IExtensionRegistry>();
            extensionRegistry.RegisterExtension<IComparable>("test1");
            extensionRegistry.RegisterExtension<IComparable>("test2");
            extensionRegistry.RegisterExtension<IComparable>("test3");

            Assert.NotNull(extensionRegistry);
            IComparable[] results = extensionRegistry.GetExtensions<IComparable>().ToArray();
            Assert.Equal(3, results.Length);
        }

        [Theory]
        [InlineData(typeof(IJobHostContextFactory), typeof(JobHostContextFactory))]
        [InlineData(typeof(IExtensionRegistry), typeof(DefaultExtensionRegistry))]
        [InlineData(typeof(ITypeLocator), typeof(DefaultTypeLocator))]
        [InlineData(typeof(StorageClientFactory), typeof(StorageClientFactory))]
        [InlineData(typeof(INameResolver), typeof(DefaultNameResolver))]
        [InlineData(typeof(IJobActivator), typeof(DefaultJobActivator))]
        public void GetService_ReturnsExpectedDefaultServices(Type serviceType, Type expectedInstanceType)
        {
            JobHostConfiguration configuration = new JobHostConfiguration();

            var service = configuration.GetService(serviceType);
            Assert.Equal(expectedInstanceType, service.GetType());
        }

        [Fact]
        public void GetService_ThrowsArgumentNull_WhenServiceTypeIsNull()
        {
            JobHostConfiguration configuration = new JobHostConfiguration();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => configuration.GetService(null)
            );
            Assert.Equal("serviceType", exception.ParamName);
        }

        [Fact]
        public void GetService_ReturnsNull_WhenServiceTypeNotFound()
        {
            JobHostConfiguration configuration = new JobHostConfiguration();

            object result = configuration.GetService(typeof(IComparable));
            Assert.Null(result);
        }

        [Fact]
        public void AddService_AddsNewService()
        {
            JobHostConfiguration configuration = new JobHostConfiguration();

            IComparable service = "test1";
            configuration.AddService<IComparable>(service);

            IComparable result = configuration.GetService<IComparable>();
            Assert.Same(service, result);
        }

        [Fact]
        public void AddService_ReplacesExistingService()
        {
            JobHostConfiguration configuration = new JobHostConfiguration();

            IComparable service = "test1";
            configuration.AddService<IComparable>(service);

            IComparable result = configuration.GetService<IComparable>();
            Assert.Same(service, result);

            IComparable service2 = "test2";
            configuration.AddService<IComparable>(service2);
            result = configuration.GetService<IComparable>();
            Assert.Same(service2, result);
        }

        [Fact]
        public void AddService_ThrowsArgumentNull_WhenServiceTypeIsNull()
        {
            JobHostConfiguration configuration = new JobHostConfiguration();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => configuration.AddService(null, "test1")
            );
            Assert.Equal("serviceType", exception.ParamName);
        }

        [Fact]
        public void AddService_ThrowsArgumentOutOfRange_WhenInstanceNotInstanceOfType()
        {
            JobHostConfiguration configuration = new JobHostConfiguration();

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => configuration.AddService(typeof(IComparable), new object())
            );
            Assert.Equal("serviceInstance", exception.ParamName);
        }

        [Fact]
        public void StorageClientFactory_GetterSetter()
        {
            JobHostConfiguration configuration = new JobHostConfiguration();

            StorageClientFactory clientFactory = configuration.StorageClientFactory;
            Assert.NotNull(clientFactory);
            Assert.Same(clientFactory, configuration.GetService<StorageClientFactory>());

            CustomStorageClientFactory customFactory = new CustomStorageClientFactory();
            configuration.StorageClientFactory = customFactory;
            Assert.Same(customFactory, configuration.StorageClientFactory);
            Assert.Same(customFactory, configuration.GetService<StorageClientFactory>());
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("Blah", false)]
        [InlineData("Development", true)]
        [InlineData("development", true)]
        public void IsDevelopment_ReturnsCorrectValue(string settingValue, bool expected)
        {
            string prev = Environment.GetEnvironmentVariable(Constants.EnvironmentSettingName);
            Environment.SetEnvironmentVariable(Constants.EnvironmentSettingName, settingValue);

            JobHostConfiguration config = new JobHostConfiguration();
            Assert.Equal(config.IsDevelopment, expected);

            Environment.SetEnvironmentVariable(Constants.EnvironmentSettingName, prev);
        }

        [Fact]
        public void UseDevelopmentSettings_ConfiguresCorrectValues()
        {
            string prev = Environment.GetEnvironmentVariable(Constants.EnvironmentSettingName);
            Environment.SetEnvironmentVariable(Constants.EnvironmentSettingName, "Development");

            JobHostConfiguration config = new JobHostConfiguration();
            Assert.False(config.UsingDevelopmentSettings);

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            Assert.True(config.UsingDevelopmentSettings);
            Assert.Equal(TraceLevel.Verbose, config.Tracing.ConsoleLevel);
            Assert.Equal(TimeSpan.FromSeconds(2), config.Queues.MaxPollingInterval);
            Assert.Equal(TimeSpan.FromSeconds(15), config.Singleton.ListenerLockPeriod);

            Environment.SetEnvironmentVariable(Constants.EnvironmentSettingName, prev);
        }

        private static void TestHostIdThrows(string hostId)
        {
            // Arrange
            JobHostConfiguration configuration = new JobHostConfiguration();

            // Act & Assert
            ExceptionAssert.ThrowsArgument(() => { configuration.HostId = hostId; }, "value",
                "A host ID must be between 1 and 32 characters, contain only lowercase letters, numbers, and " +
                "dashes, not start or end with a dash, and not contain consecutive dashes.");
        }

        private class CustomStorageClientFactory : StorageClientFactory
        {
        }
    }
}
