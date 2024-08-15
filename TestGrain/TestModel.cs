using System.ComponentModel;


namespace TestGrain
{

    [GenerateSerializer]
    public class TestModel
    {
        [Id(0)]
        public string MYCOLUM { get; set; }
    }
}
