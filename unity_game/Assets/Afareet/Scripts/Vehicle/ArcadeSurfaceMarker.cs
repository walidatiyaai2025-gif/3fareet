using UnityEngine;

namespace Afareet.Vehicle
{
    [DisallowMultipleComponent]
    public sealed class ArcadeSurfaceMarker : MonoBehaviour
    {
        [SerializeField] private ArcadeSurfaceKind surfaceKind = ArcadeSurfaceKind.Asphalt;

        public ArcadeSurfaceKind SurfaceKind => surfaceKind;

        public void Configure(ArcadeSurfaceKind kind)
        {
            surfaceKind = kind;
        }
    }
}
