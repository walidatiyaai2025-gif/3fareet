using System;
using UnityEngine;

namespace Afareet.GarageRuntime
{
    public sealed class GaragePreviewResolution
    {
        public GarageVehicleDefinition Definition { get; }
        public GameObject Prefab { get; }
        public string ResourcePath => Definition.PreviewResourcePath;

        public GaragePreviewResolution(GarageVehicleDefinition definition, GameObject prefab)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
        }
    }

    public static class GaragePreviewResourcePolicy
    {
        private static readonly string[] ForbiddenNonProductionPathSegments =
        {
            "/Generated/",
            "/Preview/",
            "/Refinement/",
            "/Review/",
            "/Blockout/",
            "/Prototype/",
            "/Debug/"
        };

        public static void ValidateProductionResourcePathOrThrow(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
                throw new ArgumentException("Garage preview resource path is required.", nameof(resourcePath));
            if (!resourcePath.StartsWith("Art/Vehicles/", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Garage preview resource '{resourcePath}' must live under Art/Vehicles/.");
            if (resourcePath.IndexOf("/Production/", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    $"Garage preview resource '{resourcePath}' must point to an explicit Production path.");

            var normalized = "/" + resourcePath.Trim('/') + "/";
            for (var index = 0; index < ForbiddenNonProductionPathSegments.Length; index++)
            {
                var forbidden = ForbiddenNonProductionPathSegments[index];
                if (normalized.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException(
                        $"Garage preview resource '{resourcePath}' is classified as non-production by segment '{forbidden}'.");
            }
        }
    }

    public sealed class GaragePreviewResolver
    {
        public GaragePreviewResolution Resolve(GarageVehicleDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            GaragePreviewResourcePolicy.ValidateProductionResourcePathOrThrow(definition.PreviewResourcePath);

            var prefab = Resources.Load<GameObject>(definition.PreviewResourcePath);
            if (prefab == null)
                throw new InvalidOperationException(
                    $"Garage production preview is missing for vehicle '{definition.Id}' at Resources/{definition.PreviewResourcePath}. " +
                    "Do not substitute a generated/review/blockout asset; register the external dependency in EXTERNAL_ASSET_REQUESTS.txt.");

            return new GaragePreviewResolution(definition, prefab);
        }

        public bool TryResolve(
            GarageVehicleDefinition definition,
            out GaragePreviewResolution resolution,
            out string error)
        {
            try
            {
                resolution = Resolve(definition);
                error = null;
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is InvalidOperationException)
            {
                resolution = null;
                error = exception.Message;
                return false;
            }
        }
    }
}
