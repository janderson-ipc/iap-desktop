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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Os.Properties;
using Google.Solutions.IapDesktop.Extensions.Os.Views.InstanceDetails;
using Google.Solutions.IapDesktop.Extensions.Os.Views.PackageInventory;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Os.Services
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    public class Extension
    {
        public Extension(IServiceProvider serviceProvider)
        {
            //
            // Add commands to project explorer.
            //
            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();

            projectExplorer.ToolbarCommands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "Show &details",
                    InstanceDetailsViewModel.GetToolbarCommandState,
                    context => serviceProvider.GetService<InstanceDetailsWindow>().ShowWindow())
                {
                    Image = Resources.ComputerDetails_16
                },
                4);

            projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "Show &details",
                    InstanceDetailsViewModel.GetContextMenuCommandState,
                    context => serviceProvider.GetService<InstanceDetailsWindow>().ShowWindow())
                {
                    Image = Resources.ComputerDetails_16
                },
                7);

            var osCommand = projectExplorer.ContextMenuCommands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "Soft&ware packages",
                    PackageInventoryViewModel.GetCommandState,
                    context => { }),
                8);
            osCommand.AddCommand(
                new Command<IProjectExplorerNode>(
                    "Show &installed packages",
                    PackageInventoryViewModel.GetCommandState,
                    context => serviceProvider.GetService<InstalledPackageInventoryWindow>().ShowWindow())
                {
                    Image = Resources.Package_16
                });
            osCommand.AddCommand(
                new Command<IProjectExplorerNode>(
                    "Show &available updates",
                    PackageInventoryViewModel.GetCommandState,
                    context => serviceProvider.GetService<AvailablePackageInventoryWindow>().ShowWindow())
                {
                    Image = Resources.PackageUpdate_16
                });

            //
            // Add commands to main menu.
            //
            var mainForm = serviceProvider.GetService<IMainForm>();
            mainForm.ViewMenu.AddCommand(
                new Command<IMainForm>(
                    "&Instance details",
                    _ => CommandState.Enabled,
                    _ => serviceProvider.GetService<InstanceDetailsWindow>().ShowWindow())
                {
                    Image = Resources.ComputerDetails_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.I
                },
                3);
            mainForm.ViewMenu.AddCommand(
                new Command<IMainForm>(
                    "I&nstalled packages",
                    _ => CommandState.Enabled,
                    _ => serviceProvider.GetService<InstalledPackageInventoryWindow>().ShowWindow())
                {
                    Image = Resources.Package_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.P
                },
                4);
            mainForm.ViewMenu.AddCommand(
                new Command<IMainForm>(
                    "&Available updates",
                    _ => CommandState.Enabled,
                    _ => serviceProvider.GetService<AvailablePackageInventoryWindow>().ShowWindow())
                {
                    Image = Resources.PackageUpdate_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.U
                },
                5);
        }
    }
}
