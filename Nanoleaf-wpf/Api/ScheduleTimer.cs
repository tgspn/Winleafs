﻿using System;
using System.Threading.Tasks;
using System.Timers;

using NLog;
using Winleafs.Api;
using Winleafs.Models.Enums;
using Winleafs.Models.Models;
using Winleafs.Wpf.Api.Effects;

namespace Winleafs.Wpf.Api
{
    public class ScheduleTimer
    {
        private readonly Timer _timer;
        private readonly Orchestrator _orchestrator;

        public ScheduleTimer(Orchestrator orchestrator)
        {
            _orchestrator = orchestrator;

            _timer = new Timer(60000);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        /// <summary>
        /// Starts the timer and fires the event to select an effect.
        /// </summary>
        public void StartTimer()
        {
            _timer.Start();
            FireTimer();
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void StopTimer()
        {
            _timer.Stop();
        }

        /// <summary>
        /// Fires the timer to set the correct effect for the current time.
        /// </summary>
        private void FireTimer()
        {
            OnTimedEvent(this, null);
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Task.Run(() => SetEffectsForDevices());
        }

        private async Task SetEffectsForDevices()
        {
            if (_orchestrator.Device.OperationMode == OperationMode.Schedule)
            {
                var activeTrigger = _orchestrator.Device.GetActiveTimeTrigger();

                if (activeTrigger == null)
                {
                    var client = NanoleafClient.GetClientForDevice(_orchestrator.Device);

                    //There are no triggers so the lights can be turned off if it is not off already
                    await client.StateEndpoint.SetStateWithStateCheckAsync(false);
                }
                else
                {
                    await _orchestrator.ActivateEffect(activeTrigger.Effect, activeTrigger.Brightness);
                }
            }
        }
    }
}
