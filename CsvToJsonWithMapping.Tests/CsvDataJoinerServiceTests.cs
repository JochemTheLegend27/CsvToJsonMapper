using CsvToJsonWithMapping.Models;
using CsvToJsonWithMapping.Services;

namespace CsvToJsonWithMapping.Tests
{
    public class CsvDataJoinerServiceTests
    {
        private void ValidateForeignKeyMatches(IEnumerable<IDictionary<string, object?>> foreignKeyData, Dictionary<string, string> expectedMatches)
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
            var csvDataJoinerService = new CsvDataJoinerService();

            var relations = new List<Relation>();
            var csvData = new Dictionary<string, IEnumerable<IDictionary<string, string?>>>
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
            var result = csvDataJoinerService.JoinCsvDataBasedOnRelations(relations, csvData);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void JoinCsvDataBasedOnRelations_ShouldThrowException_WhenPrimaryFileMissing()
        {
            // Arrange
            var csvDataJoinerService = new CsvDataJoinerService();

            var relations = new List<Relation>
            {
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "primary.csv", CSVField = "ID" },
                    ForeignKey = new Field { CSVFileName = "foreign.csv", CSVField = "FKID" }
                }
            };

            var csvData = new Dictionary<string, IEnumerable<IDictionary<string, string?>>>
            {
                {
                    "foreign.csv", new List<Dictionary<string, string?>>
                    {
                        new() { { "FKID", "1" }, { "Value", "Foreign1" } }
                    }
                }
            };

            // Act & Assert
            Assert.Throws<Exception>(() => csvDataJoinerService.JoinCsvDataBasedOnRelations(relations, csvData));
        }

        [Fact]
        public void JoinCsvDataBasedOnRelations_ShouldHandleEmptyCSVFiles()
        {
            // Arrange
            var csvDataJoinerService = new CsvDataJoinerService();

            var relations = new List<Relation>
            {
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "primary.csv", CSVField = "ID" },
                    ForeignKey = new Field { CSVFileName = "foreign.csv", CSVField = "FKID" }
                }
            };

            var csvData = new Dictionary<string, IEnumerable<IDictionary<string, string?>>>
            {
                { "primary.csv", new List<Dictionary<string, string?>>() },
                { "foreign.csv", new List<Dictionary<string, string?>>() }
            };

            // Act
            var result = csvDataJoinerService.JoinCsvDataBasedOnRelations(relations, csvData);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("primary.csv"));
            Assert.Empty(result["primary.csv"]);
        }


        [Fact]
        public void JoinCsvDataBasedOnRelations_ShouldJoinWithMultipleFKs_WhenSamePrimaryKeyMatchesMultipleFks()
        {
            // Arrange
            var csvDataJoinerService = new CsvDataJoinerService();

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

            var csvData = new Dictionary<string, IEnumerable<IDictionary<string, string?>>>
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
            var result = csvDataJoinerService.JoinCsvDataBasedOnRelations(relations, csvData);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("primary.csv", result.Keys);

            var primaryResult = result["primary.csv"];
            Assert.NotNull(primaryResult);

            foreach (var record in primaryResult)
            {
                var foreignKeyData1 = new List<Dictionary<string, object?>>();
                if (record.TryGetValue("foreign1.csv", out var foreignData1))
                {
                    foreignKeyData1 = foreignData1 as List<Dictionary<string, object?>>;
                }
                var expectedMatches1 = new Dictionary<string, string>
                {
                    { "1", "Foreign1" },
                    { "2", "Foreign2" }
                };
                ValidateForeignKeyMatches(foreignKeyData1, expectedMatches1);

                var foreignKeyData2 = new List<Dictionary<string, object?>>();
                if (record.TryGetValue("foreign2.csv", out var foreignData2))
                {
                    foreignKeyData2 = foreignData2 as List<Dictionary<string, object?>>;
                }
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
            var csvDataJoinerService = new CsvDataJoinerService();
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

            var csvData = new Dictionary<string, IEnumerable<IDictionary<string, string?>>>
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
            var result = csvDataJoinerService.JoinCsvDataBasedOnRelations(relations, csvData);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("primary.csv", result.Keys);

            var primaryResults = result["primary.csv"];
            Assert.NotNull(primaryResults);

            foreach (var record in primaryResults)
            {
                var foreignKeyData1 = new List<Dictionary<string, object?>>();
                if (record.TryGetValue("foreign1.csv", out var foreignData1))
                {
                    foreignKeyData1 = foreignData1 as List<Dictionary<string, object?>>;
                }
                var expectedMatches1 = new Dictionary<string, string>
                {
                    { "1", "Foreign1" },
                    { "2", "Foreign2" }
                };
                ValidateForeignKeyMatches(foreignKeyData1, expectedMatches1);

                var foreignKeyData2 = new List<Dictionary<string, object?>>();
                if (record.TryGetValue("foreign2.csv", out var foreignData2))
                {
                    foreignKeyData2 = foreignData2 as List<Dictionary<string, object?>>;
                }
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
            var csvDataJoinerService = new CsvDataJoinerService();
            var relations = new List<Relation>
            {
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "primary.csv", CSVField = "ID" },
                    ForeignKey = new Field { CSVFileName = null, CSVField = null }
                }
            };

            var csvData = new Dictionary<string, IEnumerable<IDictionary<string, string?>>>
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
            var result = csvDataJoinerService.JoinCsvDataBasedOnRelations(relations, csvData);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("primary.csv", result.Keys);
        }

        [Fact]
        public void JoinCsvDataBasedOnRelations_ShouldHandleMultipleRelations_WhenAForeignFileRelatesToMultiplePrimaryKeys()
        {
            // Arrange
            var csvDataJoinerService = new CsvDataJoinerService();
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

            var csvData = new Dictionary<string, IEnumerable<IDictionary<string, string?>>>
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
            var result = csvDataJoinerService.JoinCsvDataBasedOnRelations(relations, csvData);

            // Assert
            Assert.NotNull(result);

            var primary1Result = result["primary1.csv"];
            Assert.NotNull(primary1Result);

            foreach (var record in primary1Result)
            {
                var foreignKeyData = new List<Dictionary<string, object?>>();
                if (record.TryGetValue("foreign.csv", out var foreignData))
                {
                    foreignKeyData = foreignData as List<Dictionary<string, object?>>;
                }
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
                var foreignKeyData = new List<Dictionary<string, object?>>();
                if (record.TryGetValue("foreign.csv", out var foreignData))
                {
                    foreignKeyData = foreignData as List<Dictionary<string, object?>>;
                }
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
            var csvDataJoinerService = new CsvDataJoinerService();
            var relations = new List<Relation>
            {
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "customers.csv", CSVField = "CustomerId" },
                    ForeignKey = new Field { CSVFileName = "orders.csv", CSVField = "FKCustomerId" }
                },
                new Relation
                {
                    PrimaryKey = new Field { CSVFileName = "orders.csv", CSVField = "OrderId" },
                    ForeignKey = new Field { CSVFileName = "products.csv", CSVField = "FKProductId" }
                }
            };

            var csvData = new Dictionary<string, IEnumerable<IDictionary<string, string?>>>
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
            var result = csvDataJoinerService.JoinCsvDataBasedOnRelations(relations, csvData);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("customers.csv", result.Keys);

            var customerResults = result["customers.csv"];
            Assert.NotNull(customerResults);

            foreach (var record in customerResults)
            {
                if (record.TryGetValue("orders.csv", out var orderData))
                {
                    var orderDataEnumerable = orderData as IEnumerable<IDictionary<string, object?>>;

                    if (orderDataEnumerable != null)
                    {
                        foreach (var order in orderDataEnumerable)
                        {
                            if (order.TryGetValue("products.csv", out var productData))
                            {
                                var productDataEnumerable = productData as IEnumerable<IDictionary<string, object?>>;

                                Assert.NotNull(productDataEnumerable);

                                if (order["OrderId"]?.ToString() == "100")
                                {
                                    var match1 = productDataEnumerable.FirstOrDefault(p => p["FKProductId"]?.ToString() == "100");
                                    Assert.NotNull(match1);
                                    Assert.Equal("Tomatoes", match1["ProductName"]);
                                }

                                if (order["OrderId"]?.ToString() == "200")
                                {
                                    var match2 = productDataEnumerable.FirstOrDefault(p => p["FKProductId"]?.ToString() == "200");
                                    Assert.NotNull(match2);
                                    Assert.Equal("Cucumbers", match2["ProductName"]);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}