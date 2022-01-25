using RatingViewerToJson;

//for (int year = 2017; year <= DateTime.Now.Year; year++)
//{
//    for (int month = 1; month <= 12; month++)
//    {
//        using FileStream fs = File.Open($"{year}-{month.ToString().PadLeft(2, '0')}-sga-rating.json", FileMode.Create, FileAccess.Write);
//        await RatingDumper.Dump(fs, year, month);
//    }
//}

using FileStream fsLatest = File.Open("latest-sga-rating.json", FileMode.Create, FileAccess.Write);
await RatingDumper.Dump(fsLatest, DateTime.Now.Year, DateTime.Now.Month);