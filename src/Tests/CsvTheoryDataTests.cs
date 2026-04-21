// namespace GbBankValidator.Tests;
//
// [Skip("Requires external CSV file")]
// public class CsvTheoryDataTests
// {
//     [Test]
//     [Skip("Requires external CSV file")]
//     [MethodDataSource(nameof(LoadTestDataFromCsv))]
//     public async Task Validate_UsingCsvTheoryData(string sortCode, string accountNumber)
//     {
//         var result = BankValidator.Validate(sortCode, accountNumber);
//         await Assert.That(result).IsTrue();
//     }
//
//     public static IEnumerable<object[]> LoadTestDataFromCsv()
//     {
//         var csvPath = Path.Combine(@"datafile.csv");
//
//         if (!File.Exists(csvPath))
//         {
//             throw new FileNotFoundException($"CSV file not found at: {csvPath}");
//         }
//
//         var lines = File.ReadAllLines(csvPath);
//
//         foreach (var line in lines.Skip(1))
//         {
//             if (string.IsNullOrWhiteSpace(line))
//                 continue;
//
//             var parts = line.Split(',');
//             if (parts.Length >= 2)
//             {
//                 var accountNumber = parts[0].Trim();
//                 var sortCode = parts[1].Trim();
//
//                 yield return [sortCode, accountNumber];
//             }
//         }
//     }
// }
//
