// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using FluentAssertions;
using InstanceService.Api.Utilities;
using System.Linq.Dynamic.Core;

namespace InstanceService.Api.Tests.Utilities
{
    /// <summary>
    /// Tests for the <see cref="CypherToLinqTranslator"/> class.
    /// </summary>
    public class CypherToLinqTranslatorTests
    {
        private readonly CypherToLinqTranslator _cypherTranslator;

        public CypherToLinqTranslatorTests()
        {
            _cypherTranslator = new CypherToLinqTranslator();
        }

        #region Return
        [Fact]
        public void Result_ReturnTrue_WhenNodename_InsideReturn()
        {
            // Arrange
            var input = "MATCH (a)<-[r]-(b)\r\nWHERE r.Label CONTAINS 'Building'\r\nRETURN a";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);

            // Assert
            Assert.True(result.ReturnContainsObject);
            Assert.False(result.ReturnContainsPredicate);
            Assert.False(result.ReturnContainsSubject);
        }

        [Fact]
        public void Result_ReturnTrue_WhenVariableNames_UsedWithColon()
        {
            // Arrange
            var input = "MATCH (a:Instance)<-[r:hasBuilding]-(b:OtherInstance)\r\nWHERE r.Label CONTAINS 'Building'\r\nRETURN a";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);

            // Assert
            Assert.True(result.ReturnContainsObject);
            Assert.False(result.ReturnContainsPredicate);
            Assert.False(result.ReturnContainsSubject);
        }

        [Fact]
        public void Result_ReturnTrue_WhenNodenames_InsideReturn()
        {
            // Arrange
            var input = "MATCH (instance1)<-[r]-(instance2)\r\nWHERE r.Label CONTAINS 'Building'\r\nRETURN instance1, instance2";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);

            // Assert
            Assert.True(result.ReturnContainsObject);
            Assert.True(result.ReturnContainsSubject);
            Assert.False(result.ReturnContainsPredicate);
        }

        [Fact]
        public void AlternativeWritingStyles_AreValid()
        {
            // Arrange
            var input = "MATCH (a)<-[r]-(b)\r\nWHERE (r.Label='hasBuilding' OR r.Label='hasRelation') AND b.Name='building1'\r\nRETURN a";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);
            var res = result.LinqWhere;

            // Assert
            Assert.Contains($"( {nameof(TestTuple.Predicate)}.Label==\"hasBuilding\" || {nameof(TestTuple.Predicate)}.Label==\"hasRelation\" ) && {nameof(TestTuple.Subject)}.Name==\"building1\"", result.LinqWhere);
        }
        #endregion

        #region StringOperations

        #region Contains
        [Fact]
        public void ContainsReplaced_WhenWhere_IsValidStatement_Using_ContainsAndSpaces()
        {
            // Arrange
            var input = "MATCH (instance1)<-[r]-(instance2)\r\nWHERE r.Label CONTAINS 'Building Tower'\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);

            // Assert
            Assert.Contains($"{nameof(TestTuple.Predicate)}.Label.Contains(\"Building Tower\")", result.LinqWhere);
        }

        [Fact]
        public void ContainsReplaced_WhenWhere_IsValidStatement_Using_Contains()
        {
            // Arrange
            var input = "MATCH (instance1)<-[r]-(instance2)\r\nWHERE r.Label CONTAINS 'Building'\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);

            // Assert
            Assert.DoesNotContain($"{nameof(TestTuple.Object)}.Label.Contains(\"Building\")", result.LinqWhere);
        }

        [Fact]
        public void Result_ContainsBuilding_WhenWhere_IsValidStatement_Using_Contains()
        {
            // Arrange
            var building = "Building";
            var address = "Address";
            var relation = "hasBuilding";
            var data = SetupTestData(building, relation, address);   
            var input = "MATCH (instance1)-[relation]->(instance2)\r\nWHERE relation.Label CONTAINS 'Building'\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };
            var queryAble = data.AsQueryable();

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);
            var linqResult = queryAble.Where(result.LinqWhere);

            // Assert
            Assert.Equal(building, linqResult.FirstOrDefault()?.Object.Name);
            Assert.Equal(relation, linqResult.FirstOrDefault()?.Predicate.Label);
            Assert.Equal(address, linqResult.FirstOrDefault()?.Subject.Name);
        }
        #endregion

        #region EndsWith
        [Fact]
        public void EndsWithReplaced_WhenWhere_IsValidStatement_Using_EndsWith()
        {
            // Arrange
            var input = "MATCH (instance1)<-[relation]-(instance2)\r\nWHERE relation.Label ENDS WITH 'Building'\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);

            // Assert
            Assert.DoesNotContain($"{nameof(TestTuple.Object)}.Label.EndsWith(\"Building\")", result.LinqWhere);
        }

        [Fact]
        public void Result_ReturnTrue_WhenWhere_IsValidStatement_Using_EndsWith()
        {
            // Arrange
            var building = "Building";
            var address = "Address";
            var relation = "hasBuilding";
            var data = SetupTestData(building, relation, address);
            var input = "MATCH (instance1)<-[relation]-(instance2)\r\nWHERE relation.Label ENDS WITH 'Building'\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };
            var queryAble = data.AsQueryable();

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);
            var linqResult = queryAble.Where(result.LinqWhere);

            // Assert
            Assert.Equal(building, linqResult.FirstOrDefault()?.Object.Name);
            Assert.Equal(relation, linqResult.FirstOrDefault()?.Predicate.Label);
            Assert.Equal(address, linqResult.FirstOrDefault()?.Subject.Name);
        }
        #endregion

        #region StartsWith
        [Fact]
        public void StartsWithReplaced_WhenWhere_IsValidStatement_Using_StartsWith()
        {
            // Arrange
            var input = "MATCH (instance1)-[relation]->(instance2)\r\nWHERE relation.Label STARTS WITH 'has'\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);

            // Assert
            Assert.DoesNotContain($"{nameof(TestTuple.Object)}.Label.StartsWith(\"has\")", result.LinqWhere);
        }

        [Fact]
        public void Result_ReturnTrue_WhenWhere_IsValidStatement_Using_StartsWith()
        {
            // Arrange
            var building = "Building";
            var address = "Address";
            var relation = "hasBuilding";
            var data = SetupTestData(building, relation, address);
            var input = "MATCH (instance1)-[relation]->(instance2)\r\nWHERE relation.Label STARTS WITH 'has'\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };
            var queryAble = data.AsQueryable();

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);
            var linqResult = queryAble.Where(result.LinqWhere);

            // Assert
            Assert.Equal(3, linqResult.Count());
        }
        #endregion

        #region LogicalOperators

        [Fact]
        public void LogicalOperator_And_InString_Replaced_WithLinqEquivalent()
        {
            // Arrange
            var input = "MATCH (instance1)<-[r]-(instance2)\r\nWHERE instance1.Name AND r.Label\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);

            // Assert
            Assert.Contains("&&", result.LinqWhere);
        }

        [Fact]
        public void LogicalOperator_Or_InString_Replaced_WithLinqEquivalent()
        {
            // Arrange
            var input = "MATCH (instance1)<-[r]-(instance2)\r\nWHERE instance1.Name OR r.Label\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);

            // Assert
            Assert.Contains("||", result.LinqWhere);
        }

        [Fact]
        public void LogicalOperator_NotEqual_InString_Replaced_WithLinqEquivalent()
        {
            // Arrange
            var input = "MATCH (instance1)<-[r]-(instance2)\r\nWHERE instance1.Name NOT EQUAL 'Building1'\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);

            // Assert
            Assert.Contains("!=", result.LinqWhere);
        }

        [Fact]
        public void LogicalOperator_Not_InString_Replaced_WithLinqEquivalent()
        {
            // Arrange
            var input = "MATCH (instance1)<-[r]-(instance2)\r\nWHERE NOT instance1.Name = 'Building1'\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);

            // Assert
            Assert.Contains($"!({nameof(TestTuple.Object)}.Name == \"Building1\")", result.LinqWhere);
        }
        #endregion

        [Fact]
        public void Equal_InString_Replaced_WithLinqEquivalent()
        {
            // Arrange
            var input = "MATCH (instance1)<-[r]-(instance2)\r\nWHERE instance1.Name = 'Building1'\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);

            // Assert
            Assert.Contains("==", result.LinqWhere);
        }

        [Fact]
        public void VariablesInString_NotReplaced_WhenStatement_IsValid()
        {
            // Arrange
            var input = "MATCH (instance1)<-[r]-(instance2)\r\nWHERE instance1.Name CONTAINS 'DEUTSCHLAND'\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);

            // Assert
            Assert.DoesNotContain("DEUTSCHL&&", result.LinqWhere);
        }

        [Fact]
        public void Property_InString_Replaced_WithLinqEquivalent()
        {
            // Arrange
            var input = $"MATCH (instance1)<-[r]-(instance2)\r\nWHERE instance1.{nameof(Models.Instance.Properties)}.Prop CONTAINS 'Building'\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act
            var result = _cypherTranslator.InterpreteCypher(input, variableNames);

            // Assert
            Assert.Contains($".{nameof(Models.Instance.Properties)}[\"Prop\"]", result.LinqWhere);
        }

        #endregion

        #region InvalidCypher
        [Fact]
        public void ThrowsError_WhenMatch_IsNotValidStatement_WrongDirection()
        {
            // Arrange
            var input = "MATCH (instance1)<-[relation]->(instance2)\r\nWHERE instance1.Name\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _cypherTranslator.InterpreteCypher(input, variableNames));
        }

        [Fact]
        public void ThrowsError_WhenMatch_IsNotValidStatement_WrongRelationNotation()
        {
            // Arrange
            var input = "MATCH (instance1)\r\nWHERE instance1.Name\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _cypherTranslator.InterpreteCypher(input, variableNames));
        }

        [Fact]
        public void ThrowsError_WhenCypher_IsNotValid_MissingWhere()
        {
            // Arrange
            var input = "MATCH (instance1)-[relation]->(instance2)\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _cypherTranslator.InterpreteCypher(input, variableNames));
        }

        [Fact]
        public void ThrowsError_WhenCypher_IsNotValid_MissingMatch()
        {
            // Arrange
            var input = "WHERE instance1\r\nRETURN instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _cypherTranslator.InterpreteCypher(input, variableNames));
        }

        [Fact]
        public void ThrowsError_WhenCypher_IsNotValid_MissingReturn()
        {
            // Arrange
            var input = "MATCH (instance1)-[relation]->(instance2)\r\nWHERE instance1";
            var variableNames = new CypherToLinqTranslator.RelationVariableNames
            {
                Object = nameof(TestTuple.Object),
                Predicate = nameof(TestTuple.Predicate),
                Subject = nameof(TestTuple.Subject)
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _cypherTranslator.InterpreteCypher(input, variableNames));
        }
        #endregion

        private List<TestTuple> SetupTestData(string instance1, string relation, string instance2) {
            return new List<TestTuple>
            {
                new TestTuple{
                    Object = new TestInstance { Name = "otherBuilding2" },
                    Predicate = new TestRelation { Label = "hasRelation" },
                    Subject = new TestInstance { Name = "otherAddress" }
                },
                new TestTuple{
                    Object = new TestInstance { Name = instance1 },
                    Predicate = new TestRelation { Label = relation },
                    Subject = new TestInstance { Name = instance2 }
                },
                new TestTuple{
                    Object = new TestInstance { Name = "otherBuilding2" },
                    Predicate = new TestRelation { Label = "hasRelation" },
                    Subject = new TestInstance { Name = "otherCountry" }
                }
            };
        }

        private class TestTuple
        {
            public TestInstance Object { get; set; } = new();
            public TestRelation Predicate { get; set; } = new();
            public TestInstance Subject { get; set; }= new();
        }

        private class TestInstance
        {
            public string Name = string.Empty;
        }

        private class TestRelation
        {
            public string Label = string.Empty;
        }
    }
}