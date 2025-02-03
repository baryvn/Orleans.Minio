using Microsoft.Extensions.Logging;
using Orleans.Timers;
namespace TestGrain
{
    public class HelloGrain : Grain, IHelloGrain, IRemindable
    {
        private readonly ILogger _logger;

        private readonly IReminderRegistry _reminderRegistry;


        private IGrainReminder? _rTest;
        private bool _taskDone = false;

        private readonly IPersistentState<TestModel> _test;
        public HelloGrain(ILogger<HelloGrain> logger, IReminderRegistry reminderRegistry, [PersistentState("test", "test")] IPersistentState<TestModel> test)
        {
            _logger = logger;
            _test = test;
            _reminderRegistry = reminderRegistry;
        }

        public async Task<string> GetCount()
        {
            await _test.ReadStateAsync();
            return _test.State + "";
        }

        public async Task AddItem(TestModel model)
        {
            _test.State = model;
            await _test.WriteStateAsync();
        }
        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            try
            {
                if (reminderName == "TEST_REMIDER")
                {
                    // Excute task
                    if (_taskDone)
                    {
                        if (_rTest == null)
                        {
                            _rTest = await _reminderRegistry.GetReminder(GrainContext.GrainId, "TEST_REMIDER");
                        }
                        if (_rTest != null)
                            await _reminderRegistry.UnregisterReminder(GrainContext.GrainId, _rTest);
                    }
                    _taskDone = true;
                }
            }
            catch (Exception)
            {
                //log
            }
        }
        public async Task RegisterRemider()
        {
            if (_rTest == null)
            {
                _rTest = await _reminderRegistry.GetReminder(GrainContext.GrainId, "TEST_REMIDER");
            }
            if (_rTest == null)
            {
                _rTest = await _reminderRegistry.RegisterOrUpdateReminder(
                callingGrainId: GrainContext.GrainId,
                reminderName: "TEST_REMIDER",
                dueTime: TimeSpan.Zero,
                period: TimeSpan.FromMinutes(1));
            }
        }

    }
}
