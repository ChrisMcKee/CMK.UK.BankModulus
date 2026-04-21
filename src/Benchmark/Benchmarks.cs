using BenchmarkDotNet.Attributes;

using CMK.UK.BankModulus;

using ModulusChecking;

namespace Benchmark
{
    [MemoryDiagnoser]
    [KeepBenchmarkFiles]
    public class Benchmarks
    {
        private const string SortCode = "012345";
        private const string AccountNumber = "12345678";

        [Benchmark(Baseline = true)]
        public bool? ModulusChecker()
        {
            var modulusChecker = new ModulusChecker();
            return modulusChecker.CheckBankAccount(SortCode,AccountNumber);
        }

        [Benchmark]
        public bool GbBankValidator()
        {
            return BankValidator.Validate(SortCode, AccountNumber);
        }
    }
}
