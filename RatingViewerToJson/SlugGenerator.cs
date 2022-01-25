using System.Text;
using System.Text.RegularExpressions;

namespace RatingViewerToJson
{
    public static class SlugGenerator
    {
        public static string GenerateSlug(params string[] partsToBeConcatinated)
        {
            return GenerateSlug(string.Join(" ", partsToBeConcatinated));
        }

        public static string GenerateSlug(this string phrase)
        {
            string str = RemoveAccent(phrase).ToLower();

            // invalid chars
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");

            // convert multiple spaces into one space
            str = Regex.Replace(str, @"\s+", " ").Trim();

            // cut and trim
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
            str = Regex.Replace(str, @"\s", "-"); // hyphens
            return str;
        }

        public static string RemoveAccent(this string txt)
        {
            byte[] bytes = CodePagesEncodingProvider.Instance.GetEncoding("Cyrillic").GetBytes(txt);
            return Encoding.ASCII.GetString(bytes);
        }
    }
}