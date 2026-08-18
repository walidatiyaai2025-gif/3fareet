using System;
using UnityEngine;

namespace Afareet.GarageRuntime
{
    public sealed class PlayerPrefsGarageStateStorage : IGarageStateStorage
    {
        public const string DefaultKey = "afareet.garage.state.v2";
        private readonly string key;

        public PlayerPrefsGarageStateStorage(string key = DefaultKey)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Garage PlayerPrefs key must be non-blank.", nameof(key));
            this.key = key;
        }

        public bool TryRead(out string payload)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                payload = null;
                return false;
            }

            payload = PlayerPrefs.GetString(key, string.Empty);
            return true;
        }

        public void Write(string payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            PlayerPrefs.SetString(key, payload);
            PlayerPrefs.Save();
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
