using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenMod.API.Ioc;
using OpenMod.API.Plugins;
using OpenMod.Core.Helpers;
using SDG.Unturned;

namespace NewEssentials.Chat
{
    //TODO: Document and stuff
    [ServiceImplementation]
    public class BroadcastingService : IBroadcastingService
    {
        public bool IsActive { get; set; }

        private readonly IConfiguration m_Configuration;
        private int m_BroadcastIndex;
        private readonly IOpenModPlugin m_Plugin;

        public BroadcastingService(IConfiguration config, IPluginAccessor<NewEssentials> plugin)
        {
            m_Configuration = config;
            m_Plugin = plugin.Instance;
            if (m_Configuration.GetValue<int>("broadcast:repeatingBroadcastInterval") > 0)
                AsyncHelper.Schedule("NewEssentials::Broadcasting", async ()  => await Broadcast(m_Configuration.GetValue<int>("broadcast:repeatingBroadcastInterval")));
        }
        
        
        private async UniTask ClearEffectCoroutine(float time)
        {
            await UniTask.Delay((int) time);

            await UniTask.SwitchToMainThread();

            foreach (SteamPlayer player in Provider.clients.Where(x => x != null))
                EffectManager.askEffectClearByID(m_Configuration.GetValue<ushort>("broadcasting:effectId"), player.playerID.steamID);

            IsActive = false;
        }
        
        public async UniTask StartBroadcast(int duration, string msg)
        {
            await UniTask.SwitchToMainThread();
            
            foreach (var player in Provider.clients.Where(x => x != null))
                EffectManager.sendUIEffect(m_Configuration.GetValue<ushort>("broadcasting:effectId"), 4205, player.playerID.steamID, true, msg);

            IsActive = true;

            await ClearEffectCoroutine(duration);
        }

        private async UniTask Broadcast(float time)
        {
            while (m_Plugin.IsComponentAlive)
            {
                await UniTask.Delay( (int) time);

                if (IsActive)
                    await UniTask.Delay((int) time);

                List<string> messages = m_Configuration.GetValue<List<string>>("broadcasting:broadcastMessages");

                string message = messages[m_BroadcastIndex];

                await StartBroadcast(m_Configuration.GetValue<int>("broadcasting:repeatingBroadcastStayTime"), message);

                if (++m_BroadcastIndex >= messages.Count)
                    m_BroadcastIndex = 0;
            }
        }
        
    }
}