﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Winleafs.Api;
using Winleafs.Models.Models;
using Winleafs.Models.Models.Effects;

namespace Winleafs.Wpf.Api.Effects
{
    public class CustomEffects
    {
        #region static properties
        public static readonly string EffectNamePreface = "Winleafs - ";

        private static Dictionary<string, CustomEffects> _customEffectsForDevices = new Dictionary<string, CustomEffects>();

        public static CustomEffects GetCustomEffectsForDevice(Device device)
        {
            if (!_customEffectsForDevices.ContainsKey(device.IPAddress))
            {
                _customEffectsForDevices.Add(device.IPAddress, new CustomEffects(device));
            }

            return _customEffectsForDevices[device.IPAddress];
        }
        #endregion

        private Dictionary<string, ICustomEffect> _customEffects;
        private INanoleafClient _nanoleafClient;

        public CustomEffects(Device device)
        {
            _nanoleafClient = NanoleafClient.GetClientForDevice(device);

            _customEffects = new Dictionary<string, ICustomEffect>();
            _customEffects.Add($"{EffectNamePreface}Ambilight", new AmbilightEffect(_nanoleafClient));
            _customEffects.Add($"{EffectNamePreface}Turn lights off", new TurnOffEffect(_nanoleafClient));
        }

        public bool EffectIsCustomEffect(string effectName)
        {
            return _customEffects.ContainsKey(effectName);
        }

        public ICustomEffect GetCustomEffect(string effectName)
        {
            return _customEffects[effectName];
        }

        /// <summary>
        /// Returns true when any other effect next to the given effectName is active, used when switching effects. If no effectName is given, returns true when any effect is active
        /// </summary>
        public bool HasActiveEffects(string effectName = "")
        {
            return _customEffects.Any(k => !k.Key.Equals(effectName) && k.Value.IsActive());
        }

        public async Task DeactivateAllEffects()
        {
            foreach (var activeEffect in _customEffects.Where(k => k.Value.IsActive()))
            {
                await activeEffect.Value.Deactivate();
            }
        }

        public static List<Effect> GetCustomEffectAsEffects(Device device)
        {
            var customEffects = GetCustomEffectsForDevice(device);

            return customEffects._customEffects.Keys.OrderBy(n => n).Select(n => new Effect { Name = n }).ToList();
        }
    }
}