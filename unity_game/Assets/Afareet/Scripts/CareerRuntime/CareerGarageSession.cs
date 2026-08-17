using System;
using System.Collections.Generic;
using Afareet.GarageRuntime;
using Afareet.Progression;
using UnityEngine;

namespace Afareet.CareerRuntime
{
    public sealed class CareerGarageSession : MonoBehaviour
    {
        private CareerGameSession career;
        private GarageCatalog catalog;
        private GarageStateStore stateStore;
        private GarageService garage;
        private bool configured;
        private bool suppressPersistence;

        public GarageService Garage => garage;
        public GarageState State => garage?.State;
        public bool IsConfigured => configured;
        public bool RecoveredInvalidGarageSave { get; private set; }
        public bool MigratedLegacyGarageSave { get; private set; }
        public string GarageSaveRecoveryError { get; private set; }

        public event Action<GarageState> GarageStateChanged;
        public event Action GarageUnlocksChanged;

        public void Configure(
            CareerGameSession careerSession,
            IGarageStateStorage storage,
            GarageCatalog garageCatalog = null)
        {
            if (careerSession == null) throw new ArgumentNullException(nameof(careerSession));
            if (careerSession.Profile == null)
                throw new InvalidOperationException("CareerGameSession must be configured before CareerGarageSession.");
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            Unbind();
            career = careerSession;
            catalog = garageCatalog ?? GarageCatalog.CreateDefault();
            CareerGarageBridge.ValidateCareerVehicleRewardsOrThrow(
                ChapterOneCareerEventContent.CreateDefinitions(),
                catalog);

            var unlocked = CareerGarageBridge.ResolveUnlockedVehicleIds(career.Profile, catalog);
            stateStore = new GarageStateStore(storage, catalog);
            var load = stateStore.Load(unlocked);
            RecoveredInvalidGarageSave = load.RecoveredFromInvalidPayload;
            MigratedLegacyGarageSave = load.MigratedLegacyV1;
            GarageSaveRecoveryError = load.Error;

            garage = new GarageService(catalog, unlocked, load.State);
            garage.StateChanged += OnGarageStateChanged;
            career.ProgressChanged += OnCareerProgressChanged;
            configured = true;
        }

        public void ConfigureWithPlayerPrefs(
            CareerGameSession careerSession,
            GarageCatalog garageCatalog = null,
            string playerPrefsKey = PlayerPrefsGarageStateStorage.DefaultKey)
        {
            Configure(
                careerSession,
                new PlayerPrefsGarageStateStorage(playerPrefsKey),
                garageCatalog);
        }

        public IReadOnlyList<GarageVehicleAvailability> ListVehicles(bool unlockedOnly = false)
        {
            EnsureConfigured();
            return garage.ListVehicles(unlockedOnly);
        }

        public GarageVehicleDetail GetDetail(string vehicleId)
        {
            EnsureConfigured();
            return garage.GetDetail(vehicleId);
        }

        public GarageState Equip(string vehicleId)
        {
            EnsureConfigured();
            return garage.Equip(vehicleId);
        }

        public GarageState Customize(string vehicleId, GarageCosmeticSelection selection)
        {
            EnsureConfigured();
            return garage.Customize(vehicleId, selection);
        }

        public GarageState ResetCustomization(string vehicleId)
        {
            EnsureConfigured();
            return garage.ResetCustomization(vehicleId);
        }

        public void SaveNow()
        {
            EnsureConfigured();
            var unlocked = CareerGarageBridge.ResolveUnlockedVehicleIds(career.Profile, catalog);
            stateStore.Save(garage.State, unlocked);
        }

        public void RefreshCareerUnlocks()
        {
            EnsureConfigured();
            RefreshUnlocksFromCareer();
        }

        private void OnCareerProgressChanged(CareerProgress _)
        {
            if (!configured || career?.Profile == null) return;
            RefreshUnlocksFromCareer();
        }

        private void RefreshUnlocksFromCareer()
        {
            var unlocked = CareerGarageBridge.ResolveUnlockedVehicleIds(career.Profile, catalog);
            suppressPersistence = true;
            try
            {
                garage.ReplaceUnlockedVehicleIds(unlocked);
            }
            finally
            {
                suppressPersistence = false;
            }
            stateStore.Save(garage.State, unlocked);
            GarageUnlocksChanged?.Invoke();
            GarageStateChanged?.Invoke(garage.State);
        }

        private void OnGarageStateChanged(GarageState state)
        {
            if (!configured || state == null) return;
            if (!suppressPersistence)
            {
                var unlocked = CareerGarageBridge.ResolveUnlockedVehicleIds(career.Profile, catalog);
                stateStore.Save(state, unlocked);
            }
            GarageStateChanged?.Invoke(state);
        }

        private void EnsureConfigured()
        {
            if (!configured || career == null || catalog == null || stateStore == null || garage == null)
                throw new InvalidOperationException("CareerGarageSession must be configured before use.");
        }

        private void Unbind()
        {
            if (garage != null)
                garage.StateChanged -= OnGarageStateChanged;
            if (career != null)
                career.ProgressChanged -= OnCareerProgressChanged;

            configured = false;
            suppressPersistence = false;
            garage = null;
            stateStore = null;
            catalog = null;
            career = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
