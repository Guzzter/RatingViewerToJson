using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace RatingViewerToJson
{
    public partial class RatingDumper
    {
        public static async Task Dump(Stream outputStream, int year, int month)
        {
            string clubId = "060004"; // This is my favorite chess club: Schaakgenootschap Amersfoort

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://ratingviewer.nl");

            // Call #1: Get all available lists. Key is listId
            var ratingLists = new Dictionary<int, MonthlyRatingListDetails>();
            await AddRatingLists(client, ratingLists, year, month);

            // Key is containing relationship nr
            var ratings = new Dictionary<int, Rating>();

            // Call #2: Get all ratings for Seniors players
            var seniorRatingList = ratingLists.Values.SingleOrDefault(rl => rl.category == "S" && rl.year == year && rl.month == month);
            await AddRatings(client, seniorRatingList, clubId, ratings);

            // Call #3: Get all ratings for Youth players
            var youthRatingList = ratingLists.Values.SingleOrDefault(rl => rl.category == "J" && rl.year == year && rl.month == month);
            await AddRatings(client, youthRatingList, clubId, ratings);

            // Save ratings sorted by lastname and serialize to output stream
            // Note: prevent to write data to file when API returns success, but without any data
            if (ratings.Any())
            {
                JsonSerializer.Serialize(outputStream, ratings.Values.OrderBy(r => r.voornaam), new JsonSerializerOptions() { WriteIndented = true });
            }
        }

        private static async Task AddRatingLists(HttpClient client, Dictionary<int, MonthlyRatingListDetails> dict, int year, int month)
        {
            var urlListOfRatingList = "/rating-lists/index/-/{0}.json";

            int pageNumber = 1;

            while (true)
            {
                // Test if we have required Senior and Youth list for requested month & year in the list, then we can stop.
                if (dict.Values.Count(item => item.year == year && item.month == month) == 2) break;

                string ratListUrl = string.Format(urlListOfRatingList, pageNumber++);
                Console.WriteLine($"Ratinglist url: {ratListUrl}");

                // Fetch JSON and parse into JsonNode object
                string jsonString = await client.GetStringAsync(ratListUrl);
                Console.WriteLine($"Received {jsonString}");
                var jsonObject = JsonNode.Parse(jsonString);

                // Load all list detail and check if any
                JsonNode ratList = jsonObject["lists"];
                if (ratList?.AsArray() != null && ratList.AsArray().Count == 0) break;

                // Loop through all available ratings list details
                foreach (JsonObject item in ratList.AsArray())
                {
                    var listDetails = JsonSerializer.Deserialize<MonthlyRatingListDetails>(item);
                    dict.Add(listDetails.list_id, listDetails);
                }
            }
        }

        private static async Task AddRatings(HttpClient client, MonthlyRatingListDetails ratingList, string clubId, Dictionary<int, Rating> ratings)
        {
            if (ratingList == null)
                return;

            var urlRatingList = "/metrics/top/100/Rating-Delta-{0}.json?metricName=List-Position-{0}&cause_ratinglist={1}&n=100&page={3}&club={2}&rating_list={1}";

            int pageNumber = 1;

            while (true)
            {
                string ratUrl = string.Format(urlRatingList, ratingList.category, ratingList.list_id, clubId, pageNumber++);
                Console.WriteLine($"Rating url: {ratUrl}");

                // Fetch JSON and parse into JsonNode object
                string jsonString = await client.GetStringAsync(ratUrl);
                Console.WriteLine($"Received {jsonString}");
                var jsonObject = JsonNode.Parse(jsonString);

                // Load all players and check if any
                JsonNode ratNode = jsonObject["top"];
                int totalRows = jsonObject["totalRows"].GetValue<int>();

                if (ratNode?.AsArray() != null && ratNode.AsArray().Count == 0) break;

                // Loop through all players
                foreach (JsonObject item in ratNode.AsArray())
                {
                    var rat = JsonSerializer.Deserialize<Rating>(item["player"]);
                    rat.playertype = ratingList.category;
                    rat.listid = ratingList.list_id;
                    rat.deeplink = $"https://ratingviewer.nl/list/{ratingList.list_id}/players/{rat.relatienummer}";

                    // Source json has ratings as a string, but I want to use it as an int
                    int parsedRating;
                    if (int.TryParse(item["extra"]["value"]["rating_new"]?.ToString(), out parsedRating))
                    {
                        rat.rating = parsedRating;
                    }

                    int parsedRatingOriginal;
                    if (int.TryParse(item["extra"]["value"]["rating_original"]?.ToString(), out parsedRatingOriginal))
                    {
                        rat.ratingoriginal = parsedRatingOriginal;
                    }

                    int parsedRatingDelta;
                    if (int.TryParse(item["extra"]["value"]["raw_rating_change"]?.ToString(), out parsedRatingDelta))
                    {
                        rat.ratingdelta = parsedRatingDelta;
                    }

                    // Sometimes Youth players are already added as Seniors (when they play as both)
                    if (!ratings.ContainsKey(rat.relatienummer))
                    {
                        ratings.Add(rat.relatienummer, rat);
                    }
                }

                // Paged by 100, so when less items than a full page, then stop.
                if (totalRows <= 100 * pageNumber) break;
            }
        }

        public record Rating
        {
            public int relatienummer { get; init; }
            public int rating { get; set; }
            public int ratingoriginal { get; set; }
            public int ratingdelta { get; set; }

            public string voornaam { get; init; }
            public string achternaam { get; init; }
            public string tussenvoegsels { get; init; }
            public string voorletters { get; init; }
            public string federation { get; init; }
            public string slug { get { return SlugGenerator.GenerateSlug(voornaam, tussenvoegsels, achternaam); } }
            public string playertype { get; internal set; }

            public string fullname
            {
                get
                {
                    string mid = string.IsNullOrWhiteSpace(tussenvoegsels) ? "" : tussenvoegsels.Trim() + " ";
                    return $"{voornaam.Trim()} {mid}{achternaam.Trim()}";
                }
            }

            public string deeplink { get; internal set; }
            public int listid { get; internal set; }
        }

        public record MonthlyRatingListDetails
        {
            public int list_id { get; init; }
            public int year { get; init; }
            public int month { get; init; }
            public string category { get; init; }
        }
    }
}