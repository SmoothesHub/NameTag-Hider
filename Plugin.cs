using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace NameTagHider
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.smoothclient.nametaghider";
        public const string PluginName = "NameTag Hider";
        public const string PluginVersion = "1.0.0";

        private ConfigFile _customConfig;
        private ConfigEntry<bool> _hideMyName;

        private Rect _windowRect = new Rect(30f, 30f, 285f, 80f);
        private bool _showGui;
        private readonly Dictionary<Text, bool> _originalStates = new Dictionary<Text, bool>();

        private void Awake()
        {
            // Use BepInEx's actual config directory.
            string configPath = Path.Combine(Paths.ConfigPath, "NameTag Hider.cfg");

            _customConfig = new ConfigFile(configPath, true);
            _hideMyName = _customConfig.Bind(
                "General",
                "HideMyName",
                false,
                "Hide only your own nametag on this client."
            );

            // Force the file to exist immediately.
            _customConfig.Save();

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
            Logger.LogInfo($"Config: {configPath}");
            Logger.LogInfo("Press P to open/close the GUI.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
                _showGui = !_showGui;
        }

        private void LateUpdate()
        {
            if (_hideMyName.Value)
                HideLocalNameplate();
            else
                RestoreNameplate();
        }

        private void OnGUI()
        {
            if (!_showGui)
                return;

            _windowRect = GUI.Window(
                891423,
                _windowRect,
                DrawWindow,
                string.Empty
            );
        }

        private void DrawWindow(int id)
        {
            string text = _hideMyName.Value
                ? "NameTag Hider - Enabled"
                : "NameTag Hider - Disabled";

            if (GUILayout.Button(text, GUILayout.Height(42f)))
            {
                _hideMyName.Value = !_hideMyName.Value;
                _customConfig.Save();

                if (!_hideMyName.Value)
                    RestoreNameplate();
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        private void HideLocalNameplate()
        {
            try
            {
                if (GorillaTagger.Instance == null)
                    return;

                VRRig rig = GorillaTagger.Instance.offlineVRRig;
                if (rig == null)
                    return;

                // Gorilla Tag has changed the field name more than once.
                // Try the known text fields first, then fall back to a local Text scan.
                Text text = null;

                try { text = rig.playerText1; } catch { }

                if (text == null)
                {
                    Text[] texts = rig.GetComponentsInChildren<Text>(true);
                    foreach (Text candidate in texts)
                    {
                        if (candidate == null)
                            continue;

                        string objectName = candidate.gameObject.name ?? string.Empty;
                        if (objectName.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            objectName.IndexOf("player", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            text = candidate;
                            break;
                        }
                    }
                }

                if (text == null)
                    return;

                if (!_originalStates.ContainsKey(text))
                    _originalStates[text] = text.enabled;

                text.enabled = false;
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"Name hide skipped: {ex.Message}");
            }
        }

        private void RestoreNameplate()
        {
            if (_originalStates.Count == 0)
                return;

            var snapshot = new List<KeyValuePair<Text, bool>>(_originalStates);

            foreach (var entry in snapshot)
            {
                if (entry.Key != null)
                {
                    try { entry.Key.enabled = entry.Value; }
                    catch { }
                }

                _originalStates.Remove(entry.Key);
            }
        }

        private void OnDestroy()
        {
            RestoreNameplate();
        }
    }
}
