using Cysharp.Threading.Tasks;
using NewEssentials.API.Players;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using System;

namespace NewEssentials.Commands.Features
{
    [Command("items")]
    [CommandAlias("item")]
    [CommandAlias("i")]
    [CommandParent(typeof(CFeaturesRoot))]
    [CommandSyntax("[autoRepair] [autoReload]")]
    public class CFeaturesItem : UnturnedCommand
    {
        private readonly IItemFeatures m_Features;

        public CFeaturesItem(IServiceProvider serviceProvider, IItemFeatures features) : base(serviceProvider)
        {
            m_Features = features;
        }

        protected override async UniTask OnExecuteAsync()
        {
            if (Context.Parameters.Count > 2)
            {
                throw new CommandWrongUsageException(Context);
            }

            var user = (UnturnedUser)Context.Actor;

            var autoRepair = Context.Parameters.Count >= 1 && await Context.Parameters.GetAsync<bool>(0);
            var autoReload = Context.Parameters.Count == 2 && await Context.Parameters.GetAsync<bool>(1);

            m_Features.AddPlayer(user.SteamId, autoRepair, autoReload);
            // todo: show output
        }
    }
}
