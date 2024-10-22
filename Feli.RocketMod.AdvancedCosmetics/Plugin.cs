using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Feli.RocketMod.AdvancedCosmetics.Storage;
using Rocket.API.Collections;
using Rocket.Core.Assets;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Permissions;
using SDG.Provider;
using SDG.Unturned;
using Steamworks;

namespace Feli.RocketMod.AdvancedCosmetics
{
    public class Plugin : RocketPlugin
    {
        public static Plugin Instance { get; set; }
        public XMLFileAsset<PlayersCosmeticsStore> CosmeticsStore { get; set; }
        public Dictionary<int, UnturnedEconInfo> EconInfos;
        
        public override TranslationList DefaultTranslations => new TranslationList()
        {
            {"RemoveCosmetics:Fail", "You haven't set up any custom cosmetics yet"},
            {"RemoveCosmetics:Success", "Successfully removed all your cosmetics. Reconnect to the server to see the changes"},
            {"CustomCosmetic:Usage", "Correct command usage: /customcosmetics <cosmeticId> | /customcosmetics <cosmeticName> [--force (reconnects you to the server so the changes get applied)]"},
            {"CustomCosmetic:NotFound", "Cosmetic with id or name {0} was not found"},
            {"CustomCosmetic:Success", "Successfully added the cosmetic {0}"}
        };

        protected override void Load()
        {
            Instance = this;
            CosmeticsStore = new XMLFileAsset<PlayersCosmeticsStore>(Path.Combine(Directory, $"{Name}.cosmetics.xml"));
            CosmeticsStore.Load();
            UnturnedPermissions.OnJoinRequested += OnJoinRequested;
            SaveManager.onPreSave += OnPreSave;
            Logger.Log($"Advanced Cosmetics v{Assembly.GetName().Version} has been loaded");
            Logger.Log("Do you want more cool plugins? Join now: https://discord.gg/4FF2548 !");
            LoadEcon();
        }

        private void OnPreSave()
        {
            CosmeticsStore.Save();
        }

        private void OnJoinRequested(CSteamID player, ref ESteamRejection? rejectionreason)
        {
            var cosmetics = CosmeticsStore.Instance.PlayersCosmetics.FirstOrDefault(x => x.PlayerId == player.m_SteamID);
            
            if(cosmetics == null)
                return;
            
            var pending = Provider.pending.FirstOrDefault(x => x.playerID.steamID == player);
            
            cosmetics.ApplyCosmetics(pending);
        }

        protected override void Unload()
        {
            Instance = null;
            CosmeticsStore.Save();
            UnturnedPermissions.OnJoinRequested -= OnJoinRequested;
            SaveManager.onPreSave -= OnPreSave;
            Logger.Log($"Advanced Cosmetics v{Assembly.GetName().Version} has been unloaded");
        }
        void LoadEcon()
        {
            string path = Path.Combine(UnturnedPaths.RootDirectory.FullName, "EconInfo.bin");
            EconInfos = new Dictionary<int, UnturnedEconInfo>();
            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (SHA1Stream sha1Stream = new SHA1Stream(fileStream))
                    {
                        using (BinaryReader binaryReader = new BinaryReader(fileStream))
                        {
                            binaryReader.ReadInt32();
                            int num = binaryReader.ReadInt32();
                            for (int i = 0; i < num; i++)
                            {
                                UnturnedEconInfo unturnedEconInfo = new UnturnedEconInfo();
                                unturnedEconInfo.name = binaryReader.ReadString();
                                unturnedEconInfo.display_type = binaryReader.ReadString();
                                unturnedEconInfo.description = binaryReader.ReadString();
                                unturnedEconInfo.name_color = binaryReader.ReadString();
                                unturnedEconInfo.itemdefid = binaryReader.ReadInt32();
                                unturnedEconInfo.marketable = binaryReader.ReadBoolean();
                                unturnedEconInfo.scraps = binaryReader.ReadInt32();
                                unturnedEconInfo.target_game_asset_guid = new Guid(binaryReader.ReadBytes(16));
                                unturnedEconInfo.item_skin = binaryReader.ReadInt32();
                                unturnedEconInfo.item_effect = binaryReader.ReadInt32();
                                unturnedEconInfo.quality = (UnturnedEconInfo.EQuality)binaryReader.ReadInt32();
                                unturnedEconInfo.econ_type = binaryReader.ReadInt32();
                                EconInfos.Add(unturnedEconInfo.itemdefid, unturnedEconInfo);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnturnedLog.exception(e, "Caught exception loading EconInfo.bin:");
            }
        }
    }
}