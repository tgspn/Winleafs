﻿using System.IO;
using System.Collections.Generic;
using System;
using Nanoleaf_Models.Models.Scheduling;
using Newtonsoft.Json;
using System.Linq;
using Nanoleaf_Models.Enums;

namespace Nanoleaf_Models.Models
{
    public class UserSettings
    {
        public static readonly string APPLICATIONNAME = "Winleafs";

        private static readonly string SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), APPLICATIONNAME);
        private static readonly string SettingsFileName = Path.Combine(SettingsFolder, "Settings.txt");

        private static UserSettings _settings { get; set; }

        public static UserSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    LoadSettings();
                }

                return _settings;
            }
        }

        #region Stored properties
        public List<Device> Devices { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public int? SunriseHour { get; set; }
        public int? SunriseMinute { get; set; }
        public int? SunsetHour { get; set; }
        public int? SunsetMinute { get; set; }

        public bool StartAtWindowsStartup { get; set; }
        #endregion

        /// <summary>
        /// Used in the GUI to determine which device is currently being edited
        /// </summary>
        [JsonIgnore]
        public Device ActviceDevice
        {
            get
            {
                return Devices.FirstOrDefault(d => d.ActiveInGUI);
            }
        }

        public static void LoadSettings()
        {
            if (!HasSettings())
            {
                var userSettings = new UserSettings();

                userSettings.Devices = new List<Device>();

                _settings = userSettings;
            }
            else
            {
                try
                {
                    var json = File.ReadAllText(SettingsFileName);

                    var userSettings = JsonConvert.DeserializeObject<UserSettings>(json);

                    _settings = userSettings;
                }
                catch (Exception e)
                {
                    throw new SettingsFileJsonException(e);
                }
            }
        }

        public static bool HasSettings()
        {
            return File.Exists(SettingsFileName);
        }

        public void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(this);

            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            File.WriteAllText(SettingsFileName, json);
        }

        public void AddDevice(Device device)
        {
            Devices.Add(device);
            SaveSettings();
        }

        public void AddSchedule(Schedule schedule)
        {
            var device = ActviceDevice;
            device.Schedules.ForEach(s => s.Active = false);
            schedule.Active = true;

            device.Schedules.Add(schedule);
            device.Schedules = device.Schedules.OrderBy(s => s.Name).ToList();
            SaveSettings();
        }

        public void ActivateSchedule(Schedule schedule)
        {
            ActviceDevice.Schedules.ForEach(s => s.Active = false);

            schedule.Active = true;
            SaveSettings();
        }

        public void DeleteSchedule(Schedule schedule)
        {
            ActviceDevice.Schedules.Remove(schedule);
            SaveSettings();
        }

        public void UpdateSunriseSunset(int sunriseHour, int sunriseMinute, int sunsetHour, int sunsetMinute)
        {
            SunriseHour = sunriseHour;
            SunriseMinute = sunriseMinute;
            SunsetHour = sunsetHour;
            SunsetMinute = sunsetMinute;

            //Update each sunset and sunrise trigger to the new times
            foreach (var device in Devices)
            {
                foreach (var schedule in device.Schedules)
                {
                    foreach (var program in schedule.Programs)
                    {
                        foreach (var trigger in program.Triggers)
                        {
                            if (trigger.TriggerType == TriggerType.Sunrise)
                            {
                                trigger.Hours = sunriseHour;
                                trigger.Minutes = sunriseMinute;
                            }
                            else if (trigger.TriggerType == TriggerType.Sunset)
                            {
                                trigger.Hours = sunsetHour;
                                trigger.Minutes = sunsetMinute;
                            }
                        }
                    }
                }
            }
            
            SaveSettings();
        }

        /// <summary>
        /// Resets all operation modes of all device to Schedule, used at application startup (then users can switch to manual during runtime)
        /// </summary>
        public void ResetOperationModes()
        {
            foreach (var device in Devices)
            {
                device.OperationMode = OperationMode.Schedule;
            }
        }
    }

    public class SettingsFileJsonException : Exception
    {
        public SettingsFileJsonException(Exception e) : base("Error loading settings, corrupt JSON", e)
        {

        }
    }
}
