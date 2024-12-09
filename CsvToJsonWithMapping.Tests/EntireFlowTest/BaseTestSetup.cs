using CsvToJsonWithMapping.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvToJsonWithMapping.Tests.EntireFlowTest
{
    public class BaseTestSetup
    {
        protected Mock<ILogger> MockLogger { get; private set; }
        protected List<Relation> Relations { get; private set; }
        protected Mapping Mapping { get; private set; }
        protected Dictionary<string, List<Dictionary<string, string>>> CsvData { get; private set; }

        public BaseTestSetup(string relationsJson, string mappingJson, Dictionary<string, List<Dictionary<string, string>>> csvData)
        {
            MockLogger = new Mock<ILogger>();

            Relations = TestHelper.DeserializeRelations(TestHelper.GetRelationsJson(relationsJson));
            Mapping = TestHelper.DeserializeMappings(TestHelper.GetMappingJson(mappingJson));
            CsvData = TestHelper.MockCsvData(csvData);
        }
    }

}
