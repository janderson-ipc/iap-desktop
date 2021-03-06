﻿//
// Copyright 2020 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Extensions.Rdp.Views.Credentials;
using Google.Solutions.IapDesktop.Application.Test.ObjectModel;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Connection;
using Google.Solutions.IapDesktop.Extensions.Rdp.Views.ConnectionSettings;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Test.Views.Credentials
{
    [TestFixture]
    public class TestCredentialPrompt : FixtureBase
    {
        private readonly ServiceRegistry serviceRegistry = new ServiceRegistry();

        private static readonly InstanceLocator SampleInstance
            = new InstanceLocator("project-1", "zone-1", "instance-1");

        private ICredentialPrompt CreateCredentialsPrompt(
            bool isGrantedPermissionToGenerateCredentials,
            bool expectSilentCredentialGeneration,
            Mock<ITaskDialog> taskDialogMock)
        {
            this.serviceRegistry.AddSingleton<ITaskDialog>(taskDialogMock.Object);
            this.serviceRegistry.AddMock<IConnectionSettingsWindow>();
            this.serviceRegistry.AddMock<IShowCredentialsDialog>();

            var credentialsService = this.serviceRegistry.AddMock<ICredentialsService>();
            credentialsService.Setup(s => s.GenerateCredentialsAsync(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<InstanceLocator>(),
                    It.IsAny<ConnectionSettingsEditor>(),
                    It.Is<bool>(silent => silent == expectSilentCredentialGeneration)))
                .Callback(
                    (IWin32Window owner,
                    InstanceLocator instanceRef,
                    ConnectionSettingsEditor settings,
                    bool silent) =>
                    {
                        settings.Username = "bob";
                        settings.CleartextPassword = "secret";
                    });
            credentialsService.Setup(s => s.IsGrantedPermissionToGenerateCredentials(
                    It.IsAny<InstanceLocator>()))
                .ReturnsAsync(isGrantedPermissionToGenerateCredentials);

            return new CredentialPrompt(serviceRegistry);
        }

        //---------------------------------------------------------------------
        // Behavior = Allow.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenNoCredentialsFoundAndBehaviorSetToAllow_ThenGenerateOptionIsShown(
            [Values(true, false)] bool isGrantedPermissionToGenerateCredentials)
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);

            var credentialPrompt = CreateCredentialsPrompt(
                isGrantedPermissionToGenerateCredentials, 
                false,
                taskDialog);
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Allow;

            await credentialPrompt.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true);

            Assert.AreEqual("bob", settings.Username);
            Assert.AreEqual("secret", settings.Password.AsClearText());
            Assert.IsNull(settings.Domain);

            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IList<string>>(options => options.Count == 3),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Once);
        }

        [Test]
        public async Task WhenCredentialsFoundAndBehaviorSetToAllow_ThenGenerateOptionIsShown(
            [Values(true, false)] bool isGrantedPermissionToGenerateCredentials)
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);

            var credentialPrompt = CreateCredentialsPrompt(
                isGrantedPermissionToGenerateCredentials, 
                false, 
                taskDialog);
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Allow;
            settings.Username = "alice";
            settings.CleartextPassword = "alicespassword";

            await credentialPrompt.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true);

            Assert.AreEqual("bob", settings.Username);
            Assert.AreEqual("secret", settings.Password.AsClearText());
            Assert.IsNull(settings.Domain);

            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IList<string>>(options => options.Count == 2),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Once);
        }

        //---------------------------------------------------------------------
        // Behavior = AllowIfNoCredentialsFound.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenNoCredentialsFoundAndPermissionGrantedAndBehaviorSetToAllowIfNoCredentialsFound_ThenGenerateOptionIsShown()
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);

            var credentialPrompt = CreateCredentialsPrompt(true, false, taskDialog);
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound;

            await credentialPrompt.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true);

            Assert.AreEqual("bob", settings.Username);
            Assert.AreEqual("secret", settings.Password.AsClearText());
            Assert.IsNull(settings.Domain);

            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IList<string>>(options => options.Count == 3),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Once);
        }

        [Test]
        public void WhenNoCredentialsFoundAndPermissionNotGrantedAndBehaviorSetToAllowIfNoCredentialsFound_ThenJumpToSettingsOptionIsShown()
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);

            var credentialPrompt = CreateCredentialsPrompt(false, false, taskDialog);
            var window = this.serviceRegistry.AddMock<IConnectionSettingsWindow>();
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound;

            AssertEx.ThrowsAggregateException<TaskCanceledException>(
                () => credentialPrompt.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true).Wait());

            window.Verify(w => w.ShowWindow(), Times.Once);
            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IList<string>>(options => options.Count == 2),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Once);
        }

        [Test]
        public async Task WhenCredentialsFoundAndBehaviorSetToAllowIfNoCredentialsFound_ThenDialogIsSkipped(
            [Values(true, false)] bool isGrantedPermissionToGenerateCredentials)
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);

            var credentialPrompt = CreateCredentialsPrompt(
                isGrantedPermissionToGenerateCredentials,
                false, 
                taskDialog);
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.AllowIfNoCredentialsFound;
            settings.Username = "alice";
            settings.CleartextPassword = "alicespassword";

            await credentialPrompt.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true);

            Assert.AreEqual("alice", settings.Username);
            Assert.AreEqual("alicespassword", settings.Password.AsClearText());
            Assert.IsNull(settings.Domain);

            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Never);
        }

        //---------------------------------------------------------------------
        // Behavior = Disallow.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoCredentialsFoundAndBehaviorSetToDisallowAndJumpToSettingsAllowed_ThenJumpToSettingsOptionIsShown()
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);

            var credentialPrompt = CreateCredentialsPrompt(true, false, taskDialog);
            var window = this.serviceRegistry.AddMock<IConnectionSettingsWindow>();
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Disallow;

            AssertEx.ThrowsAggregateException<TaskCanceledException>(
                () => credentialPrompt.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true).Wait());

            window.Verify(w => w.ShowWindow(), Times.Once);
            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IList<string>>(options => options.Count == 2),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Once);
        }

        [Test]
        public async Task WhenNoCredentialsFoundAndBehaviorSetToDisallowAndJumpToSettingsNotAllowed_ThenDialogIsSkipped()
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(1);

            var credentialPrompt = CreateCredentialsPrompt(true, false, taskDialog);
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Disallow;

            await credentialPrompt.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                false);

            Assert.IsNull(settings.Username);
            Assert.IsNull(settings.Password);
            Assert.IsNull(settings.Domain);

            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Never);
        }

        [Test]
        public async Task WhenCredentialsFoundAndBehaviorSetToDisallow_ThenDialogIsSkipped()
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);

            var credentialPrompt = CreateCredentialsPrompt(true, false, taskDialog);
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Disallow;
            settings.Username = "alice";
            settings.CleartextPassword = "alicespassword";

            await credentialPrompt.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true);

            Assert.AreEqual("alice", settings.Username);
            Assert.AreEqual("alicespassword", settings.Password.AsClearText());
            Assert.IsNull(settings.Domain);

            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Never);
        }

        //---------------------------------------------------------------------
        // Behavior = Force.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenBehaviorSetToForceAndPermissionsGranted_ThenDialogIsSkippedAndCredentialsAreGenerated()
        {
            var taskDialog = new Mock<ITaskDialog>();

            var credentialPrompt = CreateCredentialsPrompt(true, true, taskDialog);
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Force;

            await credentialPrompt.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true);

            Assert.AreEqual("bob", settings.Username);
            Assert.AreEqual("secret", settings.Password.AsClearText());
            Assert.IsNull(settings.Domain);

            // No dialog shown.
            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Never);
        }

        [Test]
        public void WhenBehaviorSetToForceAndPermissionsNotGranted_ThenJumpToSettingsOptionIsShown()
        {
            var taskDialog = new Mock<ITaskDialog>();
            taskDialog.Setup(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IList<string>>(),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny)).Returns(0);

            var credentialPrompt = CreateCredentialsPrompt(false, false, taskDialog);
            var window = this.serviceRegistry.AddMock<IConnectionSettingsWindow>();
            var settings = new ConnectionSettingsEditor(
                new VmInstanceConnectionSettings(),
                _ => { },
                null);

            settings.CredentialGenerationBehavior = RdpCredentialGenerationBehavior.Force;

            AssertEx.ThrowsAggregateException<TaskCanceledException>(
                () => credentialPrompt.ShowCredentialsPromptAsync(
                null,
                SampleInstance,
                settings,
                true).Wait());

            window.Verify(w => w.ShowWindow(), Times.Once);
            taskDialog.Verify(t => t.ShowOptionsTaskDialog(
                It.IsAny<IWin32Window>(),
                It.IsAny<IntPtr>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IList<string>>(options => options.Count == 2),
                It.IsAny<string>(),
                out It.Ref<bool>.IsAny), Times.Once);
        }
    }
}
