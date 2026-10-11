using System.Collections.Generic;

namespace AccessControl.Common.DTOs.Policy
{
    public class PolicyDto
    {
        public string Key { get; set; } = null!;
        public string Group { get; set; } = null!;
        public string Label { get; set; } = null!;
        public string Description { get; set; } = null!;

        /// <summary>Boolean, Number o Choice.</summary>
        public string Kind { get; set; } = null!;

        public string Value { get; set; } = null!;
        public string DefaultValue { get; set; } = null!;
        public bool IsDefault { get; set; }
        public int? Min { get; set; }
        public int? Max { get; set; }
        public List<PolicyOptionDto>? Options { get; set; }
    }

    public class PolicyOptionDto
    {
        public string Value { get; set; } = null!;
        public string Label { get; set; } = null!;
    }
}
