using UnityEngine;

namespace BNG
{
    public class WeightedGrabPoint
    {
        public readonly GrabPoint GrabPoint;
        public readonly float Dist;
        public readonly float Angle;

        private const float AngleWeightMultiplier = 2f;
        private const float DistWeightMultiplier = 1f;

        public WeightedGrabPoint(GrabPoint grabPoint, float dist, float angle)
        {
            GrabPoint = grabPoint;
            Dist = dist;
            Angle = angle;
        }

        public float CalculateWeight(float maxDistance)
        {
            float normalizedAngle = Angle / 180f;
            float angleWeight = (1f - normalizedAngle) * AngleWeightMultiplier;

            float normalizedDist = Dist / maxDistance;
            float distWeight = (1f - normalizedDist) * DistWeightMultiplier;

            float finalWeight = angleWeight + distWeight;
            
            // Debug.Log(
            //     $">> {GrabPoint.name}, Angle: {Angle}, Dist: {Dist}, " +
            //     $"angleWeight: {angleWeight}, distWeight: {distWeight} " +
            //     $" maxDis: {maxDistance}, finalWeight: {finalWeight}"
            // );
            
            return finalWeight;
        }
    }
}