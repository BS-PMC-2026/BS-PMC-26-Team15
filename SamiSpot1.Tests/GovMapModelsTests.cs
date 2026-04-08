using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamiSpot.Services;
using System.Linq;
using System.Text.Json;

namespace SamiSpot.Tests.Services
{
    [TestClass]
    public class GovMapModelsTests
    {
        [TestMethod]
        public void GovMapRequest_ShouldStorePropertiesCorrectly()
        {
            var request = new GovMapRequest
            {
                Point = new double[] { 34.5, 31.2 },
                Tolerance = 15
            };

            request.Layers.Add(new GovMapLayerRequest { LayerId = "123" });

            Assert.AreEqual(2, request.Point.Length);
            Assert.AreEqual(34.5, request.Point[0], 0.0001);
            Assert.AreEqual(31.2, request.Point[1], 0.0001);
            Assert.AreEqual(1, request.Layers.Count);
            Assert.AreEqual("123", request.Layers[0].LayerId);
            Assert.AreEqual(15, request.Tolerance, 0.0001);
        }

        [TestMethod]
        public void GovMapLayerRequest_ShouldStoreLayerIdCorrectly()
        {
            var layerRequest = new GovMapLayerRequest
            {
                LayerId = "226453"
            };

            Assert.AreEqual("226453", layerRequest.LayerId);
        }

        [TestMethod]
        public void GovMapLayerResponse_ShouldStoreDataCorrectly()
        {
            var response = new GovMapLayerResponse();
            response.Data.Add(new GovMapLayerData
            {
                LayerName = "Shelters",
                LayerId = "1"
            });

            Assert.AreEqual(1, response.Data.Count);
            Assert.AreEqual("Shelters", response.Data[0].LayerName);
            Assert.AreEqual("1", response.Data[0].LayerId);
        }

        [TestMethod]
        public void GovMapLayerData_ShouldStoreEntitiesCorrectly()
        {
            var layerData = new GovMapLayerData
            {
                LayerName = "Shelters",
                LayerId = "226453"
            };

            layerData.Entities.Add(new GovMapEntity { ObjectId = 7 });

            Assert.AreEqual("Shelters", layerData.LayerName);
            Assert.AreEqual("226453", layerData.LayerId);
            Assert.AreEqual(1, layerData.Entities.Count);
            Assert.AreEqual(7, layerData.Entities[0].ObjectId);
        }

        [TestMethod]
        public void GovMapEntity_ShouldStoreFieldsAndCentroidCorrectly()
        {
            using var doc = JsonDocument.Parse(@"{ ""x"": 34.5, ""y"": 31.2 }");
            var centroidJson = doc.RootElement.Clone();

            var entity = new GovMapEntity
            {
                ObjectId = 99,
                Centroid = centroidJson
            };

            entity.Fields.Add(new GovMapField
            {
                FieldName = "Name",
                FieldValue = "Shelter A"
            });

            Assert.AreEqual(99, entity.ObjectId);
            Assert.AreEqual(JsonValueKind.Object, entity.Centroid.ValueKind);
            Assert.AreEqual(1, entity.Fields.Count);
            Assert.AreEqual("Name", entity.Fields[0].FieldName);
            Assert.AreEqual("Shelter A", entity.Fields[0].FieldValue?.ToString());
        }

        [TestMethod]
        public void GovMapCentroid_ShouldStoreCoordinatesCorrectly()
        {
            var centroid = new GovMapCentroid
            {
                X = 34.5,
                Y = 31.2
            };

            Assert.AreEqual(34.5, centroid.X, 0.0001);
            Assert.AreEqual(31.2, centroid.Y, 0.0001);
        }

        [TestMethod]
        public void GovMapField_ShouldStoreFieldValuesCorrectly()
        {
            var field = new GovMapField
            {
                FieldName = "Address",
                FieldValue = "Ashkelon"
            };

            Assert.AreEqual("Address", field.FieldName);
            Assert.AreEqual("Ashkelon", field.FieldValue?.ToString());
        }

        [TestMethod]
        public void GovMapLayerResponse_ShouldDeserializeCorrectly()
        {
            string json = @"{
                ""data"": [
                    {
                        ""layerName"": ""Shelters"",
                        ""layerId"": ""123"",
                        ""entities"": [
                            {
                                ""objectId"": 1,
                                ""centroid"": { ""x"": 34.5, ""y"": 31.2 },
                                ""fields"": [
                                    {
                                        ""fieldName"": ""Name"",
                                        ""fieldValue"": ""Shelter A""
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }";

            var result = JsonSerializer.Deserialize<GovMapLayerResponse>(json);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(1, result.Data.Count);

            var layer = result.Data.First();
            Assert.AreEqual("Shelters", layer.LayerName);
            Assert.AreEqual("123", layer.LayerId);

            Assert.IsNotNull(layer.Entities);
            Assert.AreEqual(1, layer.Entities.Count);

            var entity = layer.Entities.First();
            Assert.AreEqual(1, entity.ObjectId);

            Assert.IsNotNull(entity.Fields);
            Assert.AreEqual(1, entity.Fields.Count);

            var field = entity.Fields.First();
            Assert.AreEqual("Name", field.FieldName);
            Assert.AreEqual("Shelter A", field.FieldValue?.ToString());
        }
    }
}