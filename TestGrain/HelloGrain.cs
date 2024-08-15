using Microsoft.Extensions.Logging;
namespace TestGrain
{
    public class HelloGrain : Grain, IHelloGrain
    {
        private readonly ILogger _logger;

        private readonly IPersistentState<TestModel> _test;
        public HelloGrain(ILogger<HelloGrain> logger, [PersistentState("test", "test")] IPersistentState<TestModel> test)
        {
            _logger = logger;
            _test = test;
        }

        public async Task<string> GetCount()
        {
            await _test.ReadStateAsync();
            return _test.State.ToString();
        }

        public async Task AddItem(TestModel model)
        {
            _test.State = model;
            await _test.WriteStateAsync();
        }


    }
}
