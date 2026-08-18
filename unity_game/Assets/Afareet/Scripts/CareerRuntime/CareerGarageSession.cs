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
        private ICareerGarageVehicleRuntime vehicleRuntime;
        private bool configured;
        private bool suppressPersistence;

        public GarageService Garage => garage;
        public GarageState State => garage?.State;
        public string ActiveRuntimeVehicleId => vehicleRuntime?.ActiveVehicleId;
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
            Configure(
                careerSession,
                storage,
                new PassiveCareerGarageVehicleRuntime(),
                garageCatalog);
        }

        public void Configure(
            CareerGameSession careerSession,
            IGarageStateStorage storage,
            ICareerGarageVehicleRuntime garageVehicleRuntime,
            GarageCatalog garageCatalog = null)
        {
            if (careerSession == null) throw new ArgumentNullException(nameof(careerSession));
            if (careerSession.Profile == null)
                throw new InvalidOperationException("CareerGameSession must be configured before CareerGarageSession.");
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (garageVehicleRuntime == null) throw new ArgumentNullException(nameof(garageVehicleRuntime));

            Unbind();
            career = careerSession;
            catalog = garageCatalog ?? GarageCatalog.CreateDefault();
            vehicleRuntime = garageVehicleRuntime;
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
            vehicleRuntime.ValidateApply(garage.State.EquippedVehicleId);
            vehicleRuntime.ApplyEquippedVehicle(garage.State.EquippedVehicleId);
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
                new PassiveCareerGarageVehicleRuntime(),
                garageCatalog);
        }

        public void ConfigureWithPlayerPrefs(
            CareerGameSession careerSession,
            ICareerGarageVehicleRuntime garageVehicleRuntime,
            GarageCatalog garageCatalog = null,
            string playerPrefsKey = PlayerPrefsGarageStateStorage.DefaultKey)
        {
            Configure(
                careerSession,
                new PlayerPrefsGarageStateStorage(playerPrefsKey),
                garageVehicleRuntime,
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
            vehicleRuntime.ValidateApply(vehicleId);
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
            EnsureRuntimeEquippedVehicle(garage.State);
            stateStore.Save(garage.State, unlocked);
            GarageUnlocksChanged?.Invoke();
            GarageStateChanged?.Invoke(garage.State);
        }

        private void OnGarageStateChanged(GarageState state)
        {
            if (!configured || state == null) return;
            EnsureRuntimeEquippedVehicle(state);
            if (!suppressPersistence)
            {
                var unlocked = CareerGarageBridge.ResolveUnlockedVehicleIds(career.Profile, catalog);
                stateStore.Save(state, unlocked);
            }
            GarageStateChanged?.Invoke(state);
        }

        private void EnsureRuntimeEquippedVehicle(GarageState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (StringComparer.Ordinal.Equals(vehicleRuntime.ActiveVehicleId, state.EquippedVehicleId))
                return;
            vehicleRuntime.ValidateApply(state.EquippedVehicleId);
            vehicleRuntime.ApplyEquippedVehicle(state.EquippedVehicleId);
        }

        private void EnsureConfigured()
        {
            if (!configured || career == null || catalog == null || stateStore == null || garage == null || vehicleRuntime == null)
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
            vehicleRuntime = null;
            career = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
