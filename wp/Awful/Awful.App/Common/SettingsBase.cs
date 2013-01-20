using System;
using System.IO.IsolatedStorage;

namespace Awful.Common
{
	 public abstract class SettingsBase : BindableBase
    {
        private readonly IsolatedStorageSettings _settings;

        public SettingsBase()
        {
            if (!System.ComponentModel.DesignerProperties.IsInDesignTool)
            {
                this._settings = IsolatedStorageSettings.ApplicationSettings;
            }
        }

        public abstract void LoadSettings();

        public virtual void SaveSettings()
        {
            if (this._settings != null)
                this._settings.Save();
        }

        protected void AddOrUpdateValue(string key, object value)
        {
            if (this._settings == null)
                return;

            if (_settings.Contains(key))
                _settings[key] = value;
            else
                _settings.Add(key, value);
        }

        protected T GetValueOrDefault<T>(string key, T defaultValue)
        {
            if (this._settings == null)
                return defaultValue;

            if (!_settings.Contains(key))
            {
                _settings.Add(key, defaultValue);
                return defaultValue;
            }

            try
            {
                T value = (T)_settings[key];
                return value;
            }
            catch (Exception)
            {
                _settings[key] = defaultValue;
                return defaultValue;
            }
        }
    }
}