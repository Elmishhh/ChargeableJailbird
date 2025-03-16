using BepInEx;
using BepInEx.Logging;
using ChargeableJailbird;
using Steamworks.Ugc;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using LethalLib;
using LethalLib.Modules;
using ChargeableJailbird.Behaviours;

namespace ChargeableJailbird
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private static Item jailbirditemprops;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            LoadAssetBundle();

            Patch();
        }

        private void Patch()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        private void LoadAssetBundle()
        {
            using (Stream bundlestream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ChargeableJailbird.Resources.jailbirdassets"))
            {
                AssetBundle bundle = AssetBundle.LoadFromStream(bundlestream);
                if (bundle == null)
                {
                    Logger.LogError("JAILBIRD BUNDLE IS NULL");
                    return;
                }
                jailbirditemprops = bundle.LoadAsset<Item>("JailbirdItemProps");
                if (jailbirditemprops == null)
                {
                    Logger.LogError("JAILBIRD ITEM IS NULL");
                    return;
                }
                ChargeableJailbird.Behaviours.ChargeableJailbird jbitem = jailbirditemprops.spawnPrefab.AddComponent<Behaviours.ChargeableJailbird>();
                jbitem.itemProperties = jailbirditemprops;
                jbitem.grabbable = true;
                jbitem.hitSFX = new AudioClip[1];
                jbitem.hitSFX[0] = bundle.LoadAsset<AudioClip>("Normal_Hit");
                jbitem.jailbirdAudio = jailbirditemprops.spawnPrefab.transform.GetComponent<AudioSource>();
                jbitem.jailbirdAudio.spatialBlend = 0.8f;
                jbitem.jailbirdAudio.spatialize = true;

                jailbirditemprops.twoHandedAnimation = true;
                jailbirditemprops.grabAnim = "HoldLung";
                jailbirditemprops.holdButtonUse = true;

                jailbirditemprops.requiresBattery = true;
                jailbirditemprops.batteryUsage = 0;
                jailbirditemprops.itemIsTrigger = true;

                jailbirditemprops.positionOffset = new Vector3(0, 0, -0.2f);
                jailbirditemprops.rotationOffset = new Vector3(270, 110, 0);
            }
            Utilities.FixMixerGroups(jailbirditemprops.spawnPrefab);

            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "???";
            Items.RegisterShopItem(jailbirditemprops, null, null, node, 300);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(jailbirditemprops.spawnPrefab);

            Logger.LogInfo("successfully set-up item");
        }
    }
}
