namespace FreeSwitchUtilities.Irv
{
    public class IsValidResult
    {
        public IsValidResult(bool isValid, string overrideInput = null)
        {
            IsValid = isValid;
            OverrideInput = overrideInput;
        }

        public bool IsValid { get; set; }
        public string OverrideInput { get; set; }

        public static implicit operator IsValidResult(bool b)
        {
            return new IsValidResult(b);
        }
    }
}