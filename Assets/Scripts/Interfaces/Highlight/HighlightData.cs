using UnityEngine;

namespace ParaMoon
{
    public class HighlightData
    {
        public string Label;
        public string Value;
        public Color ValueColor = Color.white;

        public HighlightData(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public HighlightData(string label, string value, Color valueColor)
        {
            Label = label;
            Value = value;
            ValueColor = valueColor;
        }
    }
}