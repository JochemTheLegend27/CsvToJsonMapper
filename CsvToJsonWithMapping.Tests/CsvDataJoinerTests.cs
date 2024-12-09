using CsvToJsonWithMapping.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;

namespace CsvToJsonWithMapping.Tests
{
    public class CsvDataJoinerTests
    {
        private Mock<ILogger> mockLogger;

        public CsvDataJoinerTests()
        {
            mockLogger = new Mock<ILogger>();
        }

        private void ValidateForeignKeyMatches(List<Dictionary<string, object>> foreignKeyData, Dictionary<string, string> expectedMatches)
        {
            if (foreignKeyData == null) return;

            foreach (var match in expectedMatches)
            {
                var foreignMatch = foreignKeyData.FirstOrDefault(fk => fk["FKID"]?.ToString() == match.Key);
                Assert.NotNull(foreignMatch);
                Assert.Equal(match.Value, foreignMatch["Value"]);
            }
        }

        [Fact]
        public void JoinCsvDataBasedOnRelations_ShouldReturnEmpty_WhenNoRelations()
        {
            // Arrange
            var relations = new List<Relation>();
            var csvData = new Dictionary<string, List<Dictionary<string, string?>>>
            {
                {
                    "primary.csv", new List<Dictionary<string, string?>>
                    {
                        new() { { "ID", "1" }, { "Name", "Primary1" } },
                        new() { { "ID", "2" }, { "Name", "Primary2" } }
                    }
                },
                {
                    "foreign.csv", new List<Dictionary<string, string?>>
                    {
                        new() { { "FKID", "1" }, { "Value", "Foreign1" } }
                    }
                }
            };

            // Act
            var result = CsvDataJoiner.JoinCsvDataBasedOnRelations(relations, csvData, mockLogger.Object);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void JoinCsvDataBasedOnRelations_ShouldThrowException_WhenPrimaryFileMissing()
        {
            // Arrange
            var relations = new List<Relation>
            {
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "primary.csv", CSVField = "ID" },
                    ForeignKey = new Field { CSVFileName = "foreign.csv", CSVField = "FKID" }
                }
            };

            var csvData = new Dictionary<string, List<Dictionary<string, string?>>>
            {
                {
                    "foreign.csv", new List<Dictionary<string, string?>>
                    {
                        new() { { "FKID", "1" }, { "Value", "Foreign1" } }
                    }
                }
            };

            // Act & Assert
            Assert.Throws<Exception>(() => CsvDataJoiner.JoinCsvDataBasedOnRelations(relations, csvData, mockLogger.Object));
        }

        [Fact]
        public void JoinCsvDataBasedOnRelations_ShouldHandleEmptyCSVFiles()
        {
            // Arrange
            var relations = new List<Relation>
            {
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "primary.csv", CSVField = "ID" },
                    ForeignKey = new Field { CSVFileName = "foreign.csv", CSVField = "FKID" }
                }
            };

            var csvData = new Dictionary<string, List<Dictionary<string, string?>>>
            {
                { "primary.csv", new List<Dictionary<string, string?>>() }, // Empty primary CSV
                { "foreign.csv", new List<Dictionary<string, string?>>() }  // Empty foreign CSV
            };

            // Act
            var result = CsvDataJoiner.JoinCsvDataBasedOnRelations(relations, csvData, mockLogger.Object);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("primary.csv"));
            Assert.Empty(result["primary.csv"]);
        }


        [Fact]
        public void JoinCsvDataBasedOnRelations_ShouldJoinWithMultipleFKs_WhenSamePrimaryKeyMatchesMultipleFks()
        {
            // Arrange
            var relations = new List<Relation>
            {
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "primary.csv", CSVField = "ID" },
                    ForeignKey = new Field { CSVFileName = "foreign1.csv", CSVField = "FKID" }
                },
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "primary.csv", CSVField = "ID" },
                    ForeignKey = new Field { CSVFileName = "foreign2.csv", CSVField = "FKID" }
                }
            };

            var csvData = new Dictionary<string, List<Dictionary<string, string?>>>
            {
                {
                    "primary.csv", new List<Dictionary<string, string?>>
                    {
                        new() { { "ID", "1" }, { "Name", "Primary1" } },
                    }
                },
                {
                    "foreign1.csv", new List<Dictionary<string, string?>>
                    {
                        new() { { "FKID", "1" }, { "Value", "Foreign1" } },
                    }
                },
                {
                    "foreign2.csv", new List<Dictionary<string, string?>>
                    {
                        new() { { "FKID", "1" }, { "Value", "Foreign2" } },
                    }
                }
            };

            // Act
            var result = CsvDataJoiner.JoinCsvDataBasedOnRelations(relations, csvData, mockLogger.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("primary.csv", result.Keys);

            var primaryResult = result["primary.csv"];
            Assert.NotNull(primaryResult);

            foreach (var record in primaryResult)
            {
                var foreignKeyData1 = record.GetValueOrDefault("foreign1.csv") as List<Dictionary<string, object>>;
                var expectedMatches1 = new Dictionary<string, string>
                {
                    { "1", "Foreign1" },
                    { "2", "Foreign2" }
                };
                ValidateForeignKeyMatches(foreignKeyData1, expectedMatches1);

                var foreignKeyData2 = record.GetValueOrDefault("foreign2.csv") as List<Dictionary<string, object>>;
                var expectedMatches2 = new Dictionary<string, string>
                {
                    { "2", "Foreign3" },
                    { "1", "Foreign4" }
                };
                ValidateForeignKeyMatches(foreignKeyData2, expectedMatches2);
            }
        }

        [Fact]
        public void JoinCsvDataBasedOnRelations_ShouldMatchCorrectFKsToCorrectPKs_RegardlessOfLoadOrder()
        {
            // Arrange
            var relations = new List<Relation>
            {
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "primary.csv", CSVField = "ID" },
                    ForeignKey = new Field { CSVFileName = "foreign1.csv", CSVField = "FKID" }
                },
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "primary.csv", CSVField = "ID" },
                    ForeignKey = new Field { CSVFileName = "foreign2.csv", CSVField = "FKID" }
                }
            };

            var csvData = new Dictionary<string, List<Dictionary<string, string?>>>
            {
                {
                    "primary.csv", new List<Dictionary<string, string?>>
                    {
                        new() { { "ID", "1" } },
                        new() { { "ID", "2" } }
                    }
                },
                {
                    "foreign1.csv", new List<Dictionary<string, string?>>
                    {
                        new() { { "FKID", "1" }, { "Value", "Foreign1" } },
                        new() { { "FKID", "2" }, { "Value", "Foreign2" } }
                    }
                },
                {
                    "foreign2.csv", new List<Dictionary<string, string?>>
                    {
                        new() { { "FKID", "2" }, { "Value", "Foreign3" } },
                        new() { { "FKID", "1" }, { "Value", "Foreign4" } }
                    }
                }
            };

            // Act
            var result = CsvDataJoiner.JoinCsvDataBasedOnRelations(relations, csvData, mockLogger.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("primary.csv", result.Keys);

            var primaryResults = result["primary.csv"];
            Assert.NotNull(primaryResults);

            foreach (var record in primaryResults)
            {
                var foreignKeyData1 = record.GetValueOrDefault("foreign1.csv") as List<Dictionary<string, object>>;
                var expectedMatches1 = new Dictionary<string, string>
                {
                    { "1", "Foreign1" },
                    { "2", "Foreign2" }
                };
                ValidateForeignKeyMatches(foreignKeyData1, expectedMatches1);

                var foreignKeyData2 = record.GetValueOrDefault("foreign2.csv") as List<Dictionary<string, object>>;
                var expectedMatches2 = new Dictionary<string, string>
                {
                    { "2", "Foreign3" },
                    { "1", "Foreign4" }
                };
                ValidateForeignKeyMatches(foreignKeyData2, expectedMatches2);
            }
        }

        [Fact]
        public void JoinCsvDataBasedOnRelations_ShouldHandleOnlyPrimaryKey_WhenNoForeignKeyMatchesExist()
        {
            // Arrange
            var relations = new List<Relation>
            {
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "primary.csv", CSVField = "ID" },
                    ForeignKey = new Field { CSVFileName = null, CSVField = null }
                }
            };

            var csvData = new Dictionary<string, List<Dictionary<string, string?>>>
            {
                {
                    "primary.csv", new List<Dictionary<string, string?>>
                    {
                        new() { { "ID", "1" } },
                        new() { { "ID", "2" } }
                    }
                }
            };

            // Act
            var result = CsvDataJoiner.JoinCsvDataBasedOnRelations(relations, csvData, mockLogger.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("primary.csv", result.Keys);
        }

        [Fact]
        public void JoinCsvDataBasedOnRelations_ShouldHandleMultipleRelations_WhenAForeignFileRelatesToMultiplePrimaryKeys()
        {
            // Arrange
            var relations = new List<Relation>
            {
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "primary1.csv", CSVField = "ID" },
                    ForeignKey = new Field { CSVFileName = "foreign.csv", CSVField = "FKID" }
                },
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "primary2.csv", CSVField = "ID" },
                    ForeignKey = new Field { CSVFileName = "foreign.csv", CSVField = "FKID" }
                }
            };

            var csvData = new Dictionary<string, List<Dictionary<string, string?>>>
            {
                {
                    "primary1.csv", new List<Dictionary<string, string?>>
                    {
                        new() { { "ID", "1" } },
                        new() { { "ID", "3" } }
                    }
                },
                {
                    "primary2.csv", new List<Dictionary<string, string?>>
                    {
                        new() { { "ID", "2" } },
                        new() { { "ID", "4" } }
                    }
                },
                {
                    "foreign.csv", new List<Dictionary<string, string?>>
                    {
                        new() { { "FKID", "1" }, { "Value", "Foreign1" } },
                        new() { { "FKID", "2" }, { "Value", "Foreign2" } },
                        new() { { "FKID", "3" }, { "Value", "Foreign3" } },
                        new() { { "FKID", "4" }, { "Value", "Foreign4" } }
                    }
                }
            };

            // Act
            var result = CsvDataJoiner.JoinCsvDataBasedOnRelations(relations, csvData, mockLogger.Object);

            // Assert
            Assert.NotNull(result);

            var primary1Result = result["primary1.csv"];
            Assert.NotNull(primary1Result);

            foreach (var record in primary1Result)
            {
                var foreignKeyData = record.GetValueOrDefault("foreign.csv") as List<Dictionary<string, object>>;
                if (foreignKeyData == null) continue;

                var expectedMatches = new Dictionary<string, string>
                {
                    { "1", "Foreign1" },
                    { "3", "Foreign3" }
                };

                ValidateForeignKeyMatches(foreignKeyData, expectedMatches);
            }

            // Validate for primary2Result
            var primary2Result = result["primary2.csv"];
            Assert.NotNull(primary2Result);

            foreach (var record in primary2Result)
            {
                var foreignKeyData = record.GetValueOrDefault("foreign.csv") as List<Dictionary<string, object>>;
                if (foreignKeyData == null) continue;

                var expectedMatches = new Dictionary<string, string>
                {
                    { "2", "Foreign2" },
                    { "4", "Foreign4" }
                };

                ValidateForeignKeyMatches(foreignKeyData, expectedMatches);
            }
        }


        [Fact]
        public void JoinCsvDataBasedOnRelations_ShouldHandleNestedFKRelationships()
        {
            // Arrange
            var relations = new List<Relation>
            {
                // FK1 points to PK1
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "customers.csv", CSVField = "CustomerId" },
                    ForeignKey = new Field { CSVFileName = "orders.csv", CSVField = "FKCustomerId" }
                },
                // PK1 itself has an FK pointing to another table
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "orders.csv", CSVField = "OrderId" },
                    ForeignKey = new Field { CSVFileName = "products.csv", CSVField = "FKProductId" }
                }
            };

            var csvData = new Dictionary<string, List<Dictionary<string, string?>>>
            {
                {
                    "customers.csv", new List<Dictionary<string, string?>>
                    {
                        new(){ { "CustomerId", "1" }, { "Name", "Alice" } },
                        new(){ { "CustomerId", "2" }, { "Name", "Bob" } }
                    }
                },
                {
                    "orders.csv", new List<Dictionary<string, string?>>
                    {
                        new(){ { "FKCustomerId", "1" }, { "OrderId", "100" } },
                        new(){ { "FKCustomerId", "2" }, { "OrderId", "200" } }
                    }
                },
                {
                    "products.csv", new List<Dictionary<string, string?>>
                    {
                        new(){ { "FKProductId", "100" }, { "ProductName", "Tomatoes" } },
                        new(){ { "FKProductId", "200" }, { "ProductName", "Cucumbers" } }
                    }
                }
            };

            // Act
            var result = CsvDataJoiner.JoinCsvDataBasedOnRelations(relations, csvData, mockLogger.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("customers.csv", result.Keys);

            var customerResults = result["customers.csv"];
            Assert.NotNull(customerResults);

            foreach (var record in customerResults)
            {
                var orderData = record.GetValueOrDefault("orders.csv") as List<Dictionary<string, object>>;
                if (orderData == null) continue;

                foreach (var order in orderData)
                {
                    var productData = order.GetValueOrDefault("products.csv") as List<Dictionary<string, object>>;
                    Assert.NotNull(productData);

                    if (order["OrderId"]?.ToString() == "100")
                    {
                        var match1 = productData.FirstOrDefault(p => p["FKProductId"]?.ToString() == "100");
                        Assert.NotNull(match1);
                        Assert.Equal("Tomatoes", match1["ProductName"]);
                    }

                    if (order["OrderId"]?.ToString() == "200")
                    {
                        var match2 = productData.FirstOrDefault(p => p["FKProductId"]?.ToString() == "200");
                        Assert.NotNull(match2);
                        Assert.Equal("Cucumbers", match2["ProductName"]);
                    }
                }
            }
        }


    }
}