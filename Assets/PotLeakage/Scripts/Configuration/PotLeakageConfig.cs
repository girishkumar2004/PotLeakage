using UnityEngine;

namespace PotLeakage.Configuration
{
    [CreateAssetMenu(fileName = "PotLeakageConfig", menuName = "Vedanta/Pot Leakage/Configuration", order = 1)]
    public class PotLeakageConfig : ScriptableObject
    {
        [Header("Normal Production Operating Values (Process Visualization Guide - Step 1)")]
        [Tooltip("Normal production pot operating voltage in Volts (~4.0 V as approved in Step 1 of Process Guide).")]
        public float normalProductionPotVoltage = 4.0f;

        [Tooltip("Normal production Collector Bar Temperature in °C (80–120 °C normal operating range, 100 °C nominal as approved in Step 1 of Process Guide).")]
        public float normalProductionCBTTemperature = 100f;

        [Header("Documented Emergency Limits (WI-POT/OPR/019 - For Response Procedure Only)")]
        [Tooltip("Maximum voltage monitored by technical in-charge during leakage handling.")]
        public float leakageMonitoringVoltageMaximum = 4.5f;

        [Tooltip("Voltage below which the pot is stopped during uncontrolled leakage condition.")]
        public float leakageStopVoltage = 1.2f;

        [Header("Pot Voltage Observation Stage")]
        [Tooltip("Pot voltage observation value during leakage observation (~4.2 V).")]
        public float potVoltageObservationValue = 4.2f;

        [Tooltip("Current pot voltage alias (~4.2 V).")]
        public float currentPotVoltage = 4.2f;

        [Tooltip("Duct-end maximum voltage limit during leakage response (≤ 4.5 V).")]
        public float maximumDuctEndVoltage = 4.5f;
    }
}
