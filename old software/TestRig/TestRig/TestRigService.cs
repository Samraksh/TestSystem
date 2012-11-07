using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace TestRig
{
    [ServiceContract(Namespace = "http://TestRig")]
    public interface ITestRig
    {
        [OperationContract]
        void addTest(Test t);
    }

    public class TestRigService : ITestRig
    {
        private TestStatus stestStatus;

        public void addTest(Test t)
        {
            stestStatus = TestStatus.Instance;
            stestStatus.addTest(t);
            Window1._testCollection.Add(t);
        }
    }
}
