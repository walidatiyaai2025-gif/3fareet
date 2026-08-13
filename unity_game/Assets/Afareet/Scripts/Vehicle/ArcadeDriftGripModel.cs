using System;

namespace Afareet.Vehicle
{
    public static class ArcadeDriftGripModel
    {
        public static float Evaluate(float normalGrip, float driftGrip, float driftAmount)
        {
            if (normalGrip <= 0f)
                throw new ArgumentOutOfRangeException(nameof(normalGrip));
            if (driftGrip <= 0f || driftGrip > normalGrip)
                throw new ArgumentOutOfRangeException(nameof(driftGrip));

            var amount = driftAmount < 0f ? 0f : driftAmount > 1f ? 1f : driftAmount;
            return normalGrip + ((driftGrip - normalGrip) * amount);
        }
    }
}
