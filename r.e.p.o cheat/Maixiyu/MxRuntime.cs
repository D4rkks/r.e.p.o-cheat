// Conductor cho các features port từ Mai-xiyu: Update loop, OnGUI overlays, Harmony bootstrap.
using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace r.e.p.o_cheat
{
    public class MxRuntime : MonoBehaviour
    {
        private static GameObject _go;
        private bool _harmonyApplied;
        private float _patchAtTime;

        public static void Init()
        {
            if (_go != null) return;
            _go = new GameObject("mx_runtime");
            UnityEngine.Object.DontDestroyOnLoad(_go);
            _go.AddComponent<MxRuntime>();
            Debug.Log("[MxRuntime] created");
        }

        private void Start()
        {
            if (gameObject.GetComponent<FOVEditor>() == null) gameObject.AddComponent<FOVEditor>();
            if (Hax2.unlimitedBatteryComponent == null)
            {
                var ub = gameObject.AddComponent<UnlimitedBattery>();
                Hax2.unlimitedBatteryComponent = ub;
            }
            if (gameObject.GetComponent<ForceHost>() == null) gameObject.AddComponent<ForceHost>();
            _patchAtTime = Time.time + 5f;
        }

        private void Update()
        {
            try { Aimbot.UpdateAimbot(); } catch (Exception e) { MxOnce("aimbot", e); }
            try { AutoDodge.UpdateAutoDodge(); } catch (Exception e) { MxOnce("autododge", e); }
            try { AutoPickup.UpdateAutoPickup(); } catch (Exception e) { MxOnce("autopickup", e); }
            try { AutoPilot.UpdateAutoPilot(); } catch (Exception e) { MxOnce("autopilot", e); }
            try { Enemies.UpdateFreeze(); } catch (Exception e) { MxOnce("freeze", e); }
            try { Enemies.UpdateSpeedModify(); } catch (Exception e) { MxOnce("enemyspeed", e); }
            try { CreativeMode.UpdateCreativeFlight(); } catch (Exception e) { MxOnce("creative", e); }
            try { HaulAssistant.Tick(); } catch (Exception e) { MxOnce("haulassist", e); }
            try { NativeGameApi.Tick(); } catch (Exception e) { MxOnce("nativeapi", e); }

            if (!_harmonyApplied && Time.time >= _patchAtTime && Type.GetType("SpectateCamera, Assembly-CSharp") != null)
            {
                ApplyHarmonyPatches();
                _harmonyApplied = true;
            }
        }

        private void LateUpdate()
        {
            try { ThirdPersonCamera.LateUpdateCamera(); } catch (Exception e) { MxOnce("thirdperson", e); }
        }

        private void OnGUI()
        {
            try { MiniRadar.DrawRadar(); } catch (Exception e) { MxOnce("radar", e); }
            try { if (MaiDebugCheats.drawEspBool) EspOverlay.Draw(); } catch (Exception e) { MxOnce("espoverlay", e); }
            try { if (SkeletonESP.enabled) SkeletonESP.RenderSkeletons(); } catch (Exception e) { MxOnce("skeleton", e); }
            try { HaulAssistant.Draw(); } catch (Exception e) { MxOnce("hauldraw", e); }
            try { AutoPilot.DrawPathGizmo(); } catch (Exception e) { MxOnce("pathgizmo", e); }
        }

        private void ApplyHarmonyPatches()
        {
            var harmony = new Harmony("mx_port");
            int ok = 0, fail = 0;
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0) continue;
                try { harmony.CreateClassProcessor(type).Patch(); ok++; }
                catch (Exception e) { fail++; Debug.LogWarning($"[MxRuntime] patch failed {type.Name}: {e.Message}"); }
            }
            Debug.Log($"[MxRuntime] Harmony patches: {ok} ok, {fail} failed");
        }

        private static void MxOnce(string tag, Exception e)
        {
            if (_lastError.TryGetValue(tag, out var last) && (Time.time - last) < 60f) return;
            _lastError[tag] = Time.time;
            Debug.LogWarning($"[MxRuntime:{tag}] {e.Message}");
        }

        private static System.Collections.Generic.Dictionary<string, float> _lastError = new System.Collections.Generic.Dictionary<string, float>();
    }
}
