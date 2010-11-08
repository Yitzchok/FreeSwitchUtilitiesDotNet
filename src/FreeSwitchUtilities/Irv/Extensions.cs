using System.Text;

namespace FreeSwitchUtilities.Irv
{
    public static class Extensions
    {
        public static bool IsInteger(this string toParse)
        {
            int tempInt;
            return int.TryParse(toParse, out tempInt);
        }

        public static int ToInteger(this string toParse)
        {
            return int.Parse(toParse);
        }

        public static string EnterSpaceBetweenNumbers(this int integerToSpace)
        {
            return EnterSpaceBetweenLetters(integerToSpace.ToString());
        }

        public static string EnterSpaceBetweenLetters(this string strToSpace)
        {
            var sb = new StringBuilder();
            foreach (var letter in strToSpace)
                sb.Append(letter + " ");

            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }
    }
}