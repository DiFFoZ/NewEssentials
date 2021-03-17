using OpenMod.API.Ioc;
using Steamworks;

namespace NewEssentials.API.Players
{
    [Service]
    public interface IItemFeatures
    {
        void AddPlayer(CSteamID steamId, in bool autoRepair, in bool autoReload);

        void RemovePlayer(CSteamID steamId);
    }
}
