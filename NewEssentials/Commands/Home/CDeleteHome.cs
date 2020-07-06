﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using NewEssentials.Models;
using OpenMod.API.Permissions;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using UnityEngine;
using Command = OpenMod.Core.Commands.Command;

namespace NewEssentials.Commands.Home
{
    [UsedImplicitly]
    [Command("deletehome")]
    [CommandAlias("delhome")]
    [CommandDescription("Delete a saved home")]
    [CommandSyntax("<name>")]
    [CommandActor(typeof(UnturnedUser))]
    public class CDeleteHome : Command
    {
        private readonly IPermissionChecker m_PermissionChecker;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IUserDataStore m_UserDataStore;

        public CDeleteHome(IPermissionChecker permissionChecker, IStringLocalizer stringLocalizer,
            IUserDataStore userDataStore, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            m_PermissionChecker = permissionChecker;
            m_StringLocalizer = stringLocalizer;
            m_UserDataStore = userDataStore;
        }

        protected override async Task OnExecuteAsync()
        {
            string permission = "newess.home.delete";
            if (await m_PermissionChecker.CheckPermissionAsync(Context.Actor, permission) == PermissionGrantResult.Deny)
                throw new NotEnoughPermissionException(Context, permission);

            if (Context.Parameters.Length != 1)
                throw new CommandWrongUsageException(Context);

            UnturnedUser uPlayer = (UnturnedUser) Context.Actor;
            UserData userData = await m_UserDataStore.GetUserDataAsync(uPlayer.Id, uPlayer.Type);
            
            if (!userData.Data.ContainsKey("homes"))
                throw new UserFriendlyException(m_StringLocalizer["home:no_home"]);

            var homes = (Dictionary<object, object>) userData.Data["homes"];
            string homeName = Context.Parameters[0];

            if (!homes.ContainsKey(homeName))
                throw new UserFriendlyException(m_StringLocalizer["home:invalid_home", new {Home = homeName}]);

            homes.Remove(homeName);
            userData.Data["homes"] = homes;
            await m_UserDataStore.SaveUserDataAsync(userData);

            await uPlayer.PrintMessageAsync(m_StringLocalizer["home:delete", new {Home = homeName}]);
        }
    }
}