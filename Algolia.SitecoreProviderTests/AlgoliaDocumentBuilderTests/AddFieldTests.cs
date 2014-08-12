﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Algolia.SitecoreProvider;
using Algolia.SitecoreProviderTests.Builders;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Sitecore;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.Data;
using Sitecore.FakeDb;

namespace Algolia.SitecoreProviderTests.AlgoliaDocumentBuilderTests
{
    [TestFixture]
    public class AddFieldTests
    {
        [Test]
        public void AddSimpleTextFieldTest()
        {
            // arrange
            using (var db = new Db { new ItemBuilder().WithDisplayName("test").Build() })
            {
                var item = db.GetItem("/sitecore/content/source");
                var indexable = new SitecoreIndexableItem(item);


                var context = new Mock<IProviderUpdateContext>();
                var index = new IndexBuilder()
                    .WithSimpleFieldTypeMap("text")
                    //.WithDefaultFieldReader("single-line text|multi-line text|text|memo")
                    .Build();
                context.Setup(t => t.Index).Returns(index);
                var sut = new AlgoliaDocumentBuilder(indexable, context.Object);

                var field = new SitecoreItemDataField(item.Fields[0]);

                //Act
                sut.AddField(field);
                
                //Assert
                JObject doc = sut.Document;
                Assert.AreEqual("test", (string)doc["displayname"]);
            }
        }

        [Test]
        public void StringValueShouldBeTrimmed()
        {
            // arrange
            using (var db = new Db { new ItemBuilder().WithDisplayName("  test  ").Build() })
            {
                var item = db.GetItem("/sitecore/content/source");
                var indexable = new SitecoreIndexableItem(item);


                var context = new Mock<IProviderUpdateContext>();
                var index = new IndexBuilder()
                    .WithSimpleFieldTypeMap("text")
                    .Build();
                context.Setup(t => t.Index).Returns(index);
                var sut = new AlgoliaDocumentBuilder(indexable, context.Object);

                var field = new SitecoreItemDataField(item.Fields[0]);

                //Act
                sut.AddField(field);

                //Assert
                JObject doc = sut.Document;
                Assert.AreEqual("test", (string)doc["displayname"]);
            }
        }

        [Test]
        public void AddSimpleIntFieldTest()
        {
            // arrange
            using (var db = new Db { new ItemBuilder().WithCount(10).Build() })
            {
                var item = db.GetItem("/sitecore/content/source");
                var indexable = new SitecoreIndexableItem(item);


                var context = new Mock<IProviderUpdateContext>();
                var index = new IndexBuilder()
                    .WithSimpleFieldTypeMap("number")
                    .WithNumericFieldReader("number")
                    .Build();
                context.Setup(t => t.Index).Returns(index);
                var sut = new AlgoliaDocumentBuilder(indexable, context.Object);

                var field = new SitecoreItemDataField(item.Fields[0]);

                //Act
                sut.AddField(field);

                //Assert
                JObject doc = sut.Document;
                Assert.AreEqual(10, (int)doc["count"]);
                Assert.AreEqual(JTokenType.Integer, doc["count"].Type);
            }
        }

        [Test]
        public void AddSimpleDoubleFieldTest()
        {
            // arrange
            using (var db = new Db { new ItemBuilder().WithPrice(123.456).Build() })
            {
                var item = db.GetItem("/sitecore/content/source");
                var indexable = new SitecoreIndexableItem(item);
                
                var context = new Mock<IProviderUpdateContext>();
                var index = new IndexBuilder()
                    .WithSimpleFieldTypeMap("number")
                    .WithNumericFieldReader("number")
                    .Build();
                context.Setup(t => t.Index).Returns(index);
                var sut = new AlgoliaDocumentBuilder(indexable, context.Object);

                var field = new SitecoreItemDataField(item.Fields[0]);

                //Act
                sut.AddField(field);

                //Assert
                JObject doc = sut.Document;
                Assert.AreEqual(123.456, (double)doc["price"]);
                Assert.AreEqual(JTokenType.Float, doc["price"].Type);
            }
        }


        [Test]
        public void CoreItemFieldsShouldBeLoaded()
        {
            // arrange
            using (var db = new Db { new ItemBuilder().Build() })
            {
                var item = db.GetItem("/sitecore/content/source");
                var indexable = new SitecoreIndexableItem(item);

                var context = new Mock<IProviderUpdateContext>();
                var index = new IndexBuilder()
                    .Build();
                context.Setup(t => t.Index).Returns(index);
                var sut = new AlgoliaDocumentBuilder(indexable, context.Object);

                //Act
                var actual = sut.Document;

                //Assert
                Assert.AreEqual(TestData.TestItemKey.ToLower(), (string)actual["objectID"]);
                Assert.AreEqual("/sitecore/content/source", (string)actual["path"]);
                Assert.AreEqual("source", (string)actual["name"]);
            }
        }

    }
}