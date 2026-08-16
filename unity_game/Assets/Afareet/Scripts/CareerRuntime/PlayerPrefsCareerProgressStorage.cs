using System;
using UnityEngine;

namespace Afareet.CareerRuntime
{
    public sealed class PlayerPrefsCareerProgressStorage : ICareerProgressStorage
    {
        public const string DefaultKey = "afareet.career.progress.v1";

        private readonly string key;

        public PlayerPrefsCareerProgressStorage(string key = DefaultKey)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Career PlayerPrefs key is required.", nameof(key));
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
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            PlayerPrefs.SetString(key, payload);
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
