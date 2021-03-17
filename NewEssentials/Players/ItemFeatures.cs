using Cysharp.Threading.Tasks;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using NewEssentials.API.Players;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.Unturned.Players.Equipment.Events;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Priority = OpenMod.API.Prioritization.Priority;

namespace NewEssentials.Players
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.Low)]
    public class ItemFeatures : IItemFeatures, IDisposable
    {
        private readonly Dictionary<CSteamID, (bool, bool)> m_Players;
        private readonly IDisposable m_EventListenerItemEquipped;

        public ItemFeatures(IEventBus eventBus, NewEssentials plugin)
        {
            m_Players = new();
            Patches.OnStateUpdated += Patches_OnStateUpdated;
            m_EventListenerItemEquipped =
                eventBus.Subscribe<UnturnedPlayerItemEquippedEvent>(plugin, OnPlayerItemEquipped);
        }

        private async Task OnPlayerItemEquipped(IServiceProvider serviceprovider, object? sender, UnturnedPlayerItemEquippedEvent @event)
        {
            if (!m_Players.TryGetValue(@event.Player.SteamId, out var tuple))
            {
                return;
            }

            var (autoRepair, autoReload) = tuple;
            if (!autoRepair)
            {
                return;
            }
            await UniTask.SwitchToMainThread();

            var shouldSendState = false;
            AutoRepair(@event.Player.Player.equipment, Array.Empty<byte>(), ref shouldSendState);

            if (shouldSendState)
            {
                @event.Player.Player.equipment.sendUpdateState();
            }
        }

        public void AddPlayer(CSteamID steamId, in bool autoRepair, in bool autoReload)
        {
            m_Players[steamId] = (autoRepair, autoReload);
        }

        public void RemovePlayer(CSteamID steamId)
        {
            m_Players.Remove(steamId);
        }

        private void Patches_OnStateUpdated(PlayerEquipment equipment, byte[] oldBytes)
        {
            if (!m_Players.TryGetValue(equipment.channel.owner.playerID.steamID, out var tuple))
            {
                return;
            }

            var (autoRepair, autoReload) = tuple;
            var shouldSendState = false;

            if (autoReload)
            {
                AutoReload(equipment, in oldBytes, ref shouldSendState);
            }

            if (autoRepair)
            {
                AutoRepair(equipment, in oldBytes, ref shouldSendState);
            }

            if (shouldSendState)
            {
                equipment.sendUpdateState();
            }
        }

        private void AutoRepair(PlayerEquipment equipment, in byte[] oldBytes, ref bool shouldSendState)
        {
            if (equipment.quality < 100)
            {
                equipment.quality = 100;
                equipment.sendUpdateQuality();
            }

            if (equipment.asset is ItemWeaponAsset)
            {
                // barrel quality
                var barrelId = (ushort)(oldBytes[6] | oldBytes[7] << 8);
                if (barrelId == 0 || equipment.state[16] >= 100)
                {
                    return;
                }

                equipment.state[16] = 100;
                shouldSendState = true;
            }
        }

        private void AutoReload(PlayerEquipment equipment, in byte[] oldBytes, ref bool shouldSendState)
        {
            // todo: UseableMelee is ignored
            if (equipment.useable is not UseableGun { equippedGunAsset: { infiniteAmmo: false } } gun)
            {
                return;
            }

            var ammo = equipment.state[10];
            var oldAmmo = oldBytes[10];
            if (ammo >= oldAmmo)
            {
                return;
            }

            var lastAmmoId = (ushort)(oldBytes[8] | oldBytes[9] << 8);

            var gunAsset = gun.equippedGunAsset;

            lastAmmoId = lastAmmoId == 0 ? gunAsset.getMagazineID() : lastAmmoId;
            equipment.state[17] = 100;

            equipment.state[8] = (byte)lastAmmoId;
            equipment.state[9] = (byte)(lastAmmoId >> 8);
            equipment.state[10] = oldAmmo;
            shouldSendState = true;
        }

        public void Dispose()
        {
            Patches.OnStateUpdated -= Patches_OnStateUpdated;
        }

        [HarmonyPatch]
        private static class Patches
        {
            public delegate void StateUpdated(PlayerEquipment equipment, byte[] oldBytes);
            public static event StateUpdated OnStateUpdated;

            [HarmonyPrefix]
            [HarmonyPatch(typeof(UseableGun), "fire")]
            public static void PreFire(UseableGun __instance, out byte[] __state)
            {
                __instance.player.equipment.state.CopyTo(s_Buffer, 0);
                __state = s_Buffer;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(UseableGun), "fire")]
            public static void PostFire(UseableGun __instance, byte[] __state)
            {
                OnStateUpdated?.Invoke(__instance.player.equipment, __state);
            }

            private readonly static byte[] s_Buffer = new byte[18];
        }
    }
}
