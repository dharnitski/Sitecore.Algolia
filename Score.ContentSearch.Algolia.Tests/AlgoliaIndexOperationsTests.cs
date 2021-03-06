﻿using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Score.ContentSearch.Algolia.Abstract;
using Score.ContentSearch.Algolia.Tests.Builders;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.Data;
using Sitecore.FakeDb;

namespace Score.ContentSearch.Algolia.Tests
{
    [TestFixture]
    public class AlgoliaIndexOperationsTests
    {
        private DbItem _source;

        [SetUp]
        public void SetUp()
        {
            _source = new DbItem("source", new ID(TestData.TestItemGuid));
        }

        [Test]
        public void UpdateTest()
        {
            // arrange
            using (var db = new Db {_source})
            {
                var item = db.GetItem("/sitecore/content/source");
                var indexable = new SitecoreIndexableItem(item);
                JObject doc = null;

                var context = new Mock<IProviderUpdateContext>();
                context.Setup(
                    t => t.UpdateDocument(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<IExecutionContext>()))
                    .Callback(
                        (object itemToUpdate, object criteriaForUpdate, IExecutionContext executionContext) =>
                            doc = itemToUpdate as JObject);

                var index = new IndexBuilder().Build();
                context.Setup(t => t.Index).Returns(index);

                var operations = new AlgoliaIndexOperations(index);

                //Act
                operations.Update(indexable, context.Object, new ProviderIndexConfiguration());

                //Assert
                context.Verify(
                    t => t.UpdateDocument(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<IExecutionContext>()),
                    Times.Once);
                Assert.AreEqual("en_" + TestData.TestItemKey.ToLower(), (string) doc["objectID"]);
                Assert.AreEqual("/sitecore/content/source", (string) doc["_fullpath"]);
                Assert.AreEqual("source", (string) doc["_name"]);
                Assert.AreEqual("en", (string) doc["_language"]);
                Assert.AreEqual(TestData.TestItemId.ToString(), (string) doc["_id"]);
            }
        }

        [Test]
        public void AddOprationShouldAddDocToContext()
        {
            // arrange
            using (var db = new Db {_source})
            {
                var item = db.GetItem("/sitecore/content/source");
                var indexable = new SitecoreIndexableItem(item);
                JObject doc = null;

                var context = new Mock<IProviderUpdateContext>();
                context.Setup(
                    t => t.AddDocument(It.IsAny<object>(), It.IsAny<IExecutionContext>()))
                    .Callback(
                        (object itemToUpdate, IExecutionContext executionContext) =>
                            doc = itemToUpdate as JObject);

                var index = new IndexBuilder().Build();
                context.Setup(t => t.Index).Returns(index);

                var operations = new AlgoliaIndexOperations(index);

                //Act
                operations.Add(indexable, context.Object, new ProviderIndexConfiguration());

                //Assert
                context.Verify(t => t.AddDocument(It.IsAny<object>(), It.IsAny<IExecutionContext>()), Times.Once);
                Assert.AreEqual("en_" + TestData.TestItemKey.ToLower(), (string) doc["objectID"]);
                Assert.AreEqual("/sitecore/content/source", (string) doc["_fullpath"]);
                Assert.AreEqual("source", (string) doc["_name"]);
                Assert.AreEqual("en", (string) doc["_language"]);
                Assert.AreEqual(TestData.TestItemId.ToString(), (string) doc["_id"]);
            }
        }

        [Test]
        public void AddOperationShouldLoadComputedFields()
        {
            // arrange
            using (var db = new Db {new ItemBuilder().AddSubItem().Build()})
            {
                var item = db.GetItem("/sitecore/content/source/subitem");
                var indexable = new SitecoreIndexableItem(item);
                IEnumerable<JObject> docs = null;
                
                var index = new IndexBuilder().WithParentsComputedField("parents").Build();
                var algoliaRepository = new Mock<IAlgoliaRepository>();
                algoliaRepository.Setup(
                    t => t.SaveObjectsAsync(It.IsAny<IEnumerable<JObject>>()))
                    .Callback(
                        (IEnumerable<JObject> objects) =>
                            docs = objects)
                    .ReturnsAsync(new JObject());

                var context = new AlgoliaUpdateContext(index, algoliaRepository.Object);

                var operations = new AlgoliaIndexOperations(index);

                //Act
                operations.Add(indexable, context, new ProviderIndexConfiguration());
                context.Commit();

                //Assert
                var itemDoc = docs.First(t => (string) t["_name"] == "subitem");
                var parents = (JArray) itemDoc["parents"];
                parents.Count.Should().Be(1);
                ((string) parents.First).Should().Be(TestData.TestItemId.ToString());
            }
        }

        [Test]
        public void AddOperationShouldGenerateTags()
        {
            // arrange
            using (var db = new Db { new ItemBuilder().AddSubItem().Build() })
            {
                var item = db.GetItem("/sitecore/content/source");
                var indexable = new SitecoreIndexableItem(item);
                IEnumerable<JObject> docs = null;

                var index = new IndexBuilder().WithTagsBuilderForId().Build();
                var algoliaRepository = new Mock<IAlgoliaRepository>();
                algoliaRepository.Setup(
                    t => t.SaveObjectsAsync(It.IsAny<IEnumerable<JObject>>()))
                    .Callback(
                        (IEnumerable<JObject> objects) =>
                            docs = objects)
                    .ReturnsAsync(new JObject());

                var context = new AlgoliaUpdateContext(index, algoliaRepository.Object);

                var operations = new AlgoliaIndexOperations(index);

                //Act
                operations.Add(indexable, context, new ProviderIndexConfiguration());
                context.Commit();

                //Assert
                var itemDoc = docs.First(t => (string)t["_name"] == "source");
                var tags = (JArray)itemDoc["_tags"];
                tags.Count.Should().Be(2);

                tags.ToObject<string[]>().Contains(TestData.TestItemId.ToString()).Should().BeTrue();
            }
        }

        [TestCase(10, "/sitecore/...")]
        [TestCase(1000, "/sitecore/content/source")]
        public void AddOperationShouldUseMaxFieldLength(int maxLenth, string expected)
        {
            // arrange
            using (var db = new Db { new ItemBuilder().AddSubItem().Build() })
            {
                var item = db.GetItem("/sitecore/content/source");
                var indexable = new SitecoreIndexableItem(item);
                IEnumerable<JObject> docs = null;

                var index = new IndexBuilder().WithMaxFieldLength(maxLenth).Build();
                var algoliaRepository = new Mock<IAlgoliaRepository>();
                algoliaRepository.Setup(
                    t => t.SaveObjectsAsync(It.IsAny<IEnumerable<JObject>>()))
                    .Callback(
                        (IEnumerable<JObject> objects) =>
                            docs = objects)
                    .ReturnsAsync(new JObject());

                var context = new AlgoliaUpdateContext(index, algoliaRepository.Object);

                var operations = new AlgoliaIndexOperations(index);

                //Act
                operations.Add(indexable, context, index.Configuration);
                context.Commit();

                //Assert
                var itemDoc = docs.First(t => (string)t["_name"] == "source");
                ((string) itemDoc["_fullpath"]).Should().Be(expected);
            }
        }

        [Test]
        public void AddOperationShouldAssignTemplateId()
        {
            // arrange
            using (var db = new Db { new ItemBuilder().AddSubItem().Build() })
            {
                var item = db.GetItem("/sitecore/content/source");
                var indexable = new SitecoreIndexableItem(item);
                IEnumerable<JObject> docs = null;

                var index = new IndexBuilder().WithIncludeTemplateId().Build();
                var algoliaRepository = new Mock<IAlgoliaRepository>();
                algoliaRepository.Setup(
                    t => t.SaveObjectsAsync(It.IsAny<IEnumerable<JObject>>()))
                    .Callback(
                        (IEnumerable<JObject> objects) =>
                            docs = objects)
                    .ReturnsAsync(new JObject());

                var context = new AlgoliaUpdateContext(index, algoliaRepository.Object);

                var operations = new AlgoliaIndexOperations(index);

                //Act
                operations.Add(indexable, context, index.Configuration);
                context.Commit();

                //Assert
                var itemDoc = docs.First(t => (string)t["_name"] == "source");
                ((string)itemDoc["_template"]).Should().Be(item.TemplateID.ToString());
            }
        }

        [Test]
        public void TemplateIdShoulNotBeAssignedByDefault()
        {
            // arrange
            using (var db = new Db { new ItemBuilder().AddSubItem().Build() })
            {
                var item = db.GetItem("/sitecore/content/source");
                var indexable = new SitecoreIndexableItem(item);
                IEnumerable<JObject> docs = null;

                var index = new IndexBuilder().Build();
                var algoliaRepository = new Mock<IAlgoliaRepository>();
                algoliaRepository.Setup(
                    t => t.SaveObjectsAsync(It.IsAny<IEnumerable<JObject>>()))
                    .Callback(
                        (IEnumerable<JObject> objects) =>
                            docs = objects)
                    .ReturnsAsync(new JObject());

                var context = new AlgoliaUpdateContext(index, algoliaRepository.Object);

                var operations = new AlgoliaIndexOperations(index);

                //Act
                operations.Add(indexable, context, index.Configuration);
                context.Commit();

                //Assert
                var itemDoc = docs.First(t => (string)t["_name"] == "source");
                itemDoc["_template"].Should().BeNull();
            }
        }

        //[Test]
        //public void AddOperationShouldLoadItemFields()
        //{
        //    // arrange
        //    using (var db = new Db { new ItemBuilder().WithDisplayName("Name").Build() })
        //    {
        //        var item = db.GetItem("/sitecore/content/source");
        //        var indexable = new SitecoreIndexableItem(item);
        //        IEnumerable<JObject> docs = null;

        //        var index = new IndexBuilder().WithIndexAllFields().Build();
        //        var algoliaRepository = new Mock<IAlgoliaRepository>();
        //        algoliaRepository.Setup(
        //            t => t.SaveObjectsAsync(It.IsAny<IEnumerable<JObject>>()))
        //            .Callback(
        //                (IEnumerable<JObject> objects) =>
        //                    docs = objects)
        //            .ReturnsAsync(new JObject());

        //        var context = new AlgoliaUpdateContext(index, algoliaRepository.Object);

        //        var operations = new AlgoliaIndexOperations(index);

        //        //Act
        //        operations.Add(indexable, context, new ProviderIndexConfiguration());
        //        context.Commit();

        //        //Assert
        //        var itemDoc = docs.First(t => (string)t["_name"] == "source");
        //        var value = (string)itemDoc["display name"];
        //        value.Should().Be("Name");
        //    }
        //}
    }
}
