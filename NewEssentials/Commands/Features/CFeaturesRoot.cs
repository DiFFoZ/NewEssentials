using Cysharp.Threading.Tasks;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewEssentials.Commands.Features
{
    [Command("features")]
    [CommandAlias("feature")]
    [CommandActor(typeof(UnturnedUser))]
    [CommandSyntax("<item/vehicle> [autoRepair] [autoRefill]")]
    public class CFeaturesRoot : UnturnedCommand
    {
        public CFeaturesRoot(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override UniTask OnExecuteAsync()
        {
            throw new CommandWrongUsageException(Context);
        }
    }
}
