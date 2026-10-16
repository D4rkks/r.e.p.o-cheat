using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace r.e.p.o_cheat
{
    // The game recreates the local PlayerController / PhysGrabber / PlayerHealth on every level load,
    // which silently resets everything the sliders and toggles set once. This re-checks the live values
    // a few times per second and re-applies the wanted ones whenever the game changed them.
    // A slider is only enforced once the user has moved it (its value differs from the initial one),
    // so untouched sliders never overwrite the game's own defaults.
    public static class SettingsEnforcer
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const float Interval = 0.2f;

        private enum Target { Controller, Grabber, Flashlight }

        private class Setting
        {
            public string Name;
            public Target Target;
            public FieldInfo Field;
            public Func<float> Wanted;
            public float Initial;
            public bool Touched;
        }

        private static readonly Type ControllerType = typeof(global::PlayerController);
        private static readonly FieldInfo HealthField = typeof(PlayerHealth).GetField("health", Flags);
        private static readonly FieldInfo GodModeField = typeof(PlayerHealth).GetField("godMode", Flags);
        private static readonly FieldInfo EnergyCurrentField = ControllerType.GetField("EnergyCurrent", Flags);
        private static readonly FieldInfo EnergyStartField = ControllerType.GetField("EnergyStart", Flags);
        private static readonly FieldInfo GrabStrengthField = typeof(PhysGrabber).GetField("grabStrength", Flags);
        private static readonly FieldInfo FlashlightIntensityField = typeof(FlashlightController).GetField("baseIntensity", Flags);

        private static List<Setting> settings;
        private static float initialStrength;
        private static bool strengthTouched;
        private static float nextTick;

        private static void Add(string name, Target target, Type type, string field, Func<float> wanted)
        {
            var info = type.GetField(field, Flags);
            if (info == null)
            {
                Hax2.Log1($"SettingsEnforcer: field '{field}' not found on {type.Name}, '{name}' won't be enforced.");
                return;
            }
            settings.Add(new Setting { Name = name, Target = target, Field = info, Wanted = wanted, Initial = wanted() });
        }

        private static void Init()
        {
            settings = new List<Setting>();
            Add("Speed", Target.Controller, ControllerType, "MoveSpeed", () => Hax2.sliderValue);
            Add("Stamina recharge delay", Target.Controller, ControllerType, "sprintRechargeTime", () => 1f * Hax2.staminaRechargeDelay);
            Add("Stamina recharge rate", Target.Controller, ControllerType, "sprintRechargeAmount", () => 2f * Hax2.staminaRechargeRate);
            Add("Crouch delay", Target.Controller, ControllerType, "CrouchTimeMin", () => Hax2.crouchDelay);
            Add("Crouch speed", Target.Controller, ControllerType, "CrouchSpeed", () => Hax2.crouchSpeed);
            Add("Jump force", Target.Controller, ControllerType, "JumpForce", () => Hax2.jumpForce);
            Add("Extra jumps", Target.Controller, ControllerType, "JumpExtra", () => Hax2.extraJumps);
            Add("Custom gravity", Target.Controller, ControllerType, "CustomGravity", () => Hax2.customGravity);
            Add("Slide decay", Target.Controller, ControllerType, "SlideDecay", () => Hax2.slideDecay);
            Add("Grab range", Target.Grabber, typeof(PhysGrabber), "grabRange", () => Hax2.grabRange);
            Add("Throw strength", Target.Grabber, typeof(PhysGrabber), "throwStrength", () => Hax2.throwStrength);
            if (FlashlightIntensityField != null)
                Add("Flashlight intensity", Target.Flashlight, typeof(FlashlightController), "baseIntensity", () => Hax2.flashlightIntensity);
            initialStrength = Hax2.sliderValueStrength;
        }

        public static void Tick()
        {
            if (settings == null) Init();

            // Mark sliders as touched as soon as the user moves them (checked every frame so a quick change isn't missed)
            foreach (var s in settings)
            {
                if (!s.Touched && !Mathf.Approximately(s.Wanted(), s.Initial)) s.Touched = true;
            }
            if (!strengthTouched && !Mathf.Approximately(Hax2.sliderValueStrength, initialStrength)) strengthTouched = true;

            if (Time.time < nextTick) return;
            nextTick = Time.time + Interval;

            try
            {
                var controller = global::PlayerController.instance;
                if (controller == null) return; // menu / loading screen

                PlayerAvatar avatar = controller.playerAvatarScript;
                PhysGrabber grabber = avatar != null ? avatar.physGrabber : null;
                FlashlightController flashlight = avatar != null ? avatar.flashlightController : null;
                PlayerHealth health = avatar != null ? avatar.playerHealth : null;

                foreach (var s in settings)
                {
                    if (!s.Touched) continue;
                    // Unity-null checks on the typed references so destroyed objects count as missing
                    object target = s.Target == Target.Controller ? (object)controller
                                  : s.Target == Target.Grabber ? (grabber != null ? (object)grabber : null)
                                  : (flashlight != null ? (object)flashlight : null);
                    if (target == null) continue;
                    EnforceFloat(s.Name, s.Field, target, s.Wanted());
                }

                // Strength also has a server-side part (upgrade count), so go through the existing cheat when it drifted
                if (strengthTouched && grabber != null && GrabStrengthField != null)
                {
                    float current = Convert.ToSingle(GrabStrengthField.GetValue(grabber));
                    if (!Mathf.Approximately(current, Hax2.sliderValueStrength))
                    {
                        Strength.MaxStrength();
                        Hax2.Log1($"SettingsEnforcer: re-applied Strength = {Hax2.sliderValueStrength} (was {current})");
                    }
                }

                if (health != null)
                {
                    if (Hax2.godModeActive && GodModeField != null && !(bool)GodModeField.GetValue(health))
                    {
                        GodModeField.SetValue(health, true);
                        Hax2.Log1("SettingsEnforcer: re-enabled God Mode");
                    }
                    if (Hax2.infiniteHealthActive && HealthField != null && (int)HealthField.GetValue(health) < 999999)
                    {
                        HealthField.SetValue(health, 999999);
                    }
                }

                if (Hax2.stamineState && EnergyCurrentField != null && EnergyStartField != null)
                {
                    float max = (float)EnergyStartField.GetValue(controller);
                    if ((float)EnergyCurrentField.GetValue(controller) < max) EnergyCurrentField.SetValue(controller, max);
                }
            }
            catch (Exception e)
            {
                Hax2.Log1($"SettingsEnforcer error: {e.Message}");
            }
        }

        private static void EnforceFloat(string name, FieldInfo field, object target, float wanted)
        {
            object currentRaw = field.GetValue(target);
            float current = Convert.ToSingle(currentRaw);
            if (Mathf.Approximately(current, wanted)) return;

            field.SetValue(target, Convert.ChangeType(wanted, field.FieldType));
            Hax2.Log1($"SettingsEnforcer: re-applied {name} = {wanted} (was {current})");
        }
    }
}
