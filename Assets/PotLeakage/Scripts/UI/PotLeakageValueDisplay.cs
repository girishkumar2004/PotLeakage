using TMPro;
using UnityEngine;
using PotLeakage.Configuration;

namespace PotLeakage.UI
{
    public class PotLeakageValueDisplay : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Optional ScriptableObject reference. If null, uses the local Inspector fields below.")]
        public PotLeakageConfig config;

        [Header("Local Configurable Plant Operating Values")]
        [Tooltip("Normal production pot operating voltage in Volts. Default 0 displays 'VALUE NOT CONFIGURED'.")]
        public float normalProductionPotVoltage = 0f;

        [Tooltip("Normal production Collector Bar Temperature in °C. Default 0 displays 'VALUE NOT CONFIGURED'.")]
        public float normalProductionCBTTemperature = 0f;

        [Header("UI Text References")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI valueText;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI progressText;

        public const string UnconfiguredText = "VALUE NOT CONFIGURED";

        private void Start()
        {
            if (config != null)
            {
                normalProductionPotVoltage = config.normalProductionPotVoltage;
                normalProductionCBTTemperature = config.normalProductionCBTTemperature;
            }
        }

        public void SetTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }
            if (SequenceHelperFunctions.instance != null)
            {
                SequenceHelperFunctions.instance.SetTitle(title);
            }
        }

        public void SetDescription(string description)
        {
            if (descriptionText != null)
            {
                descriptionText.text = description;
            }
            if (SequenceHelperFunctions.instance != null)
            {
                SequenceHelperFunctions.instance.SetDescription(description);
            }
        }

        public void SetProgress(string progress)
        {
            if (progressText != null)
            {
                progressText.text = progress;
            }
        }

        public void SetProgressStep(int stepIndex)
        {
            SetProgress($"TASK 0{stepIndex} / 05");
        }

        public void SetStatus(string status)
        {
            if (statusText != null)
            {
                if (!string.IsNullOrEmpty(status))
                {
                    statusText.gameObject.SetActive(true);
                    statusText.text = status;
                }
                else
                {
                    statusText.gameObject.SetActive(false);
                    statusText.text = "";
                }
            }
        }

        public void DisplayNormalStatus()
        {
            DisplayNormalStatus("NORMAL OPERATING CONDITION");
        }

        public void DisplayNormalStatus(string status)
        {
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = status;
            }
        }

        public void HideStatus()
        {
            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
                statusText.text = "";
            }
        }

        public void DisplayVoltage()
        {
            float voltage = (config != null && config.normalProductionPotVoltage > 0f)
                ? config.normalProductionPotVoltage
                : normalProductionPotVoltage;

            if (valueText != null)
            {
                valueText.gameObject.SetActive(true);
                if (voltage > 0f)
                {
                    string vStr = (voltage == Mathf.Floor(voltage) || (voltage * 10) == Mathf.Floor(voltage * 10)) ? $"{voltage:0.0} V" : $"{voltage:F2} V";
                    valueText.text = $"NORMAL PRODUCTION VALUE:\n<size=120%><b>{vStr}</b></size>";
                }
                else
                {
                    valueText.text = $"NORMAL PRODUCTION VALUE:\n<size=110%><b><color=#FFAA00>{UnconfiguredText}</color></b></size>";
                }
            }
        }

        public void DisplayCBTTemperature()
        {
            float cbt = (config != null && config.normalProductionCBTTemperature > 0f)
                ? config.normalProductionCBTTemperature
                : normalProductionCBTTemperature;

            if (valueText != null)
            {
                valueText.gameObject.SetActive(true);
                if (cbt > 0f)
                {
                    valueText.text = $"NORMAL PRODUCTION CBT TEMPERATURE:\n<size=120%><b>{cbt:F0} °C</b></size>";
                }
                else
                {
                    valueText.text = $"NORMAL PRODUCTION CBT TEMPERATURE:\n<size=110%><b><color=#FFAA00>{UnconfiguredText}</color></b></size>";
                }
            }
        }

        public string FormatVoltage(float voltage)
        {
            if (voltage > 0f)
            {
                string vStr = (voltage == Mathf.Floor(voltage) || (voltage * 10) == Mathf.Floor(voltage * 10)) ? $"{voltage:0.0} V" : $"{voltage:F2} V";
                return $"NORMAL PRODUCTION VALUE:\n<size=120%><b>{vStr}</b></size>";
            }
            return $"NORMAL PRODUCTION VALUE:\n<size=110%><b><color=#FFAA00>{UnconfiguredText}</color></b></size>";
        }

        public string FormatCBTTemperature(float cbt)
        {
            if (cbt > 0f)
            {
                return $"NORMAL PRODUCTION CBT TEMPERATURE:\n<size=120%><b>{cbt:F0} °C</b></size>";
            }
            return $"NORMAL PRODUCTION CBT TEMPERATURE:\n<size=110%><b><color=#FFAA00>{UnconfiguredText}</color></b></size>";
        }

        public void HideValueDisplay()
        {
            if (valueText != null)
            {
                valueText.gameObject.SetActive(false);
                valueText.text = "";
            }
        }

        public void ClearTaskUI()
        {
            HideValueDisplay();
            HideStatus();
        }
    }
}
