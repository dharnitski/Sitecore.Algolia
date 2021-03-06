﻿using System.Linq;
using System.Xml;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Score.ContentSearch.Algolia.Tests.Fakes;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.ComputedFields;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.ContentSearch.Maintenance.Strategies;

namespace Score.ContentSearch.Algolia.Tests.Configuration
{
    [TestFixture]
    public class AlgoliaSearchIndexTests
    {

        [Test]
        public void ShouldCreateFromConfig()
        {
            //Act
            var index = LoadIndexConfiguration("Algolia.Search.config");
            
            //Assert
            index.Should().NotBeNull();
            index.Name.Should().Be("products_unstopables");
        }

        [Test]
        public void ShouldLoadPropertyStoreFromConfig()
        {
            //Act
            var index = LoadIndexConfiguration("Algolia.Search.config");

            //Assert
            index.PropertyStore.Should().NotBeNull();
            index.PropertyStore.Should().BeOfType<IndexDatabasePropertyStore>();
            var propertyStore = index.PropertyStore as IndexDatabasePropertyStore;
            propertyStore.Key.Should().Be("products_unstopables");
            propertyStore.Database.Should().Be("core");
        }

        [Test]
        public void ShouldLoadStrategy()
        {
            //Act
            var index = LoadIndexConfiguration("Algolia.Search.config");

            //Assert
            index.Strategies.Count.Should().Be(1);
            index.Strategies.First().Should().BeOfType<SynchronousStrategy>();
        }

        [Test]
        public void ShouldUseDefaultMaxFieldLength()
        {
            //Act
            var index = LoadIndexConfiguration("Algolia.Search.config");
            var configuration = index.Configuration as AlgoliaIndexConfiguration;

            //Assert
            configuration.Should().NotBeNull();
            configuration.MaxFieldLength.Should().Be(4000);
        }

        [Test]
        public void ShouldLoadMaxFieldLength()
        {
            //Act
            var index = LoadIndexConfiguration("MaxFieldLength.config");
            var configuration = index.Configuration as AlgoliaIndexConfiguration;

            //Assert
            configuration.Should().NotBeNull();
            configuration.MaxFieldLength.Should().Be(8000);
            configuration.IncludeTemplateId.Should().BeFalse();
        }

        [Test]
        public void ShouldLoadIncludeTemplateId()
        {
            //Act
            var index = LoadIndexConfiguration("IncludeTemplateId.config");
            var configuration = index.Configuration as AlgoliaIndexConfiguration;

            //Assert
            configuration.Should().NotBeNull();
            configuration.IncludeTemplateId.Should().BeTrue();
        }

        [Test]
        public void ShouldLoadCrawler()
        {
            //Act
            var index = LoadIndexConfiguration("Algolia.Search.config");

            //Assert
            index.Crawlers.Count.Should().Be(1);
            index.Crawlers.First().Should().BeOfType<SitecoreItemCrawler>();
            var crowler = index.Crawlers.First() as SitecoreItemCrawler;
            crowler.Database.Should().Be("master");
            crowler.Root.Should().Be("/sitecore/content/Unstopables/North America/United States/home/all-products");
        }

        [Test]
        public void ShouldIncludeTemplate()
        {
            //Act
            var index = LoadIndexConfiguration("IncludeTemplate.config");

            //Assert
            index.Configuration.DocumentOptions.IncludedTemplates.Count.Should().Be(1);
            index.Configuration.DocumentOptions.IncludedTemplates.First()
                .Should()
                .Be("{9CAAECFD-3BEB-44B1-9BE5-F7E30811EF2D}");
        }

        [Test]
        public void ShouldIncludeComputedField()
        {
            //Act
            var index = LoadIndexConfiguration("ComputedField.config");

            //Assert
            index.Configuration.DocumentOptions.ComputedIndexFields.Count.Should().Be(1);
            var computedField = index.Configuration.DocumentOptions.ComputedIndexFields.First();
            computedField.Should().BeOfType<SmallCreatedDate>();
            computedField.FieldName.Should().Be("__smallcreateddate");
        }

        [Test]
        public void ShouldLoadTagsProcessor()
        {
            //Act
            var index = LoadIndexConfiguration("TagsProcessor.config");

            //Assert
            var configuration = index.Configuration as AlgoliaIndexConfiguration;
            configuration.Should().NotBeNull();

            configuration.TagsProcessor.Should().NotBeNull();
            configuration.TagsProcessor.Should().BeOfType<AlgoliaTagsProcessor>();
            
            var doc = new JObject();
            doc["_id"] = "myId";
            configuration.TagsProcessor.ProcessDocument(doc);
            doc["_tags"].First(t => t.Value<string>() == "id_myId");
            doc["_id"].Should().BeNull();
        }

        [Test]
        public void TagProcessorPreffixShouldBeDefinedInTag()
        {
            //Act
            var index = LoadIndexConfiguration("TagsProcessorSecondTagNoPreffix.config");

            //Assert
            var configuration = index.Configuration as AlgoliaIndexConfiguration;
            configuration.Should().NotBeNull();

            configuration.TagsProcessor.Should().NotBeNull();
            configuration.TagsProcessor.Should().BeOfType<AlgoliaTagsProcessor>();

            var doc = new JObject();
            doc["_language"] = "en";
            configuration.TagsProcessor.ProcessDocument(doc);
            doc["_tags"].First(t => t.Value<string>() == "en");
        }

        [Test]
        public void ShouldLoadSite()
        {
            //Act
            var index = LoadIndexConfiguration("Site.config");

            //Assert
            index.Site.Should().Be("MySite");
        }

        private AlgoliaSearchIndex LoadIndexConfiguration(string fileName)
        {
            //Arrange
            string xmlPath = @"Configuration\" + fileName;
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            XmlElement root = xmlDoc.DocumentElement;
            var configNode = root.SelectSingleNode("//configuration//sitecore//contentSearch//configuration//indexes//index");

            configNode.Should().NotBeNull();
            var factory = new FakeFactory(xmlDoc);

            //Act
            return factory.CreateObject<AlgoliaSearchIndex>(configNode);
        }

        [Test]
        public void CrawlerNoIndexTest()
        {
            //Act
            var index = LoadIndexConfiguration("CrawlerShowInSearchResults.config");

            //Assert
            var crawler = index.Crawlers.First() as AlgoliaCrawler;
            crawler.ShowInSearchResultsFieldName.Should().Be("Show In Search Results");
        }

    }
}
