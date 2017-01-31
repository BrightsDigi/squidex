﻿// ==========================================================================
//  ODataQueryTests.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Squidex.Core.Schemas;
using Squidex.Infrastructure;
using Squidex.Read.MongoDb.Contents.Visitors;
using Xunit;

// ReSharper disable SpecifyACultureInStringConversionExplicitly

namespace Squidex.Read.MongoDb.Contents
{
    public class ODataQueryTests
    {
        private readonly Schema schema = 
            Schema.Create("user", new SchemaProperties { Hints = "The User" })
                .AddOrUpdateField(new StringField(1, "firstName",
                    new StringFieldProperties { Label = "FirstName", IsLocalizable = true, IsRequired = true, AllowedValues = new[] { "1", "2" }.ToImmutableList() }))
                .AddOrUpdateField(new StringField(2, "lastName",
                    new StringFieldProperties { Hints = "Last Name" }))
                .AddOrUpdateField(new BooleanField(3, "admin",
                    new BooleanFieldProperties()))
                .AddOrUpdateField(new NumberField(4, "age",
                    new NumberFieldProperties { MinValue = 1, MaxValue = 10 }));

        private readonly IBsonSerializerRegistry registry = BsonSerializer.SerializerRegistry;
        private readonly IBsonSerializer<MongoContentEntity> serializer = BsonSerializer.SerializerRegistry.GetSerializer<MongoContentEntity>();
        private readonly HashSet<Language> languages = new HashSet<Language>
        {
            Language.EN,
            Language.DE
        };

        [Fact]
        public void Should_parse_query()
        {
            var parser = schema.ParseQuery(languages, "$filter=data/FirstName/de eq 'Sebastian'");

            Assert.NotNull(parser);
        }

        [Fact]
        public void Should_create_not_operator()
        {
            var i = F("$filter=not endswith(data/firstName/de, 'Sebastian')");
            var o = C("{ 'Data.1.de' : { '$not' : /Sebastian$/i } }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_create_starts_with_query()
        {
            var i = F("$filter=startswith(data/firstName/de, 'Sebastian')");
            var o = C("{ 'Data.1.de' : /^Sebastian/i }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_create_ends_with_query()
        {
            var i = F("$filter=endswith(data/firstName/de, 'Sebastian')");
            var o = C("{ 'Data.1.de' : /Sebastian$/i }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_create_contains_query()
        {
            var i = F("$filter=contains(data/firstName/de, 'Sebastian')");
            var o = C("{ 'Data.1.de' : /Sebastian/i }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_create_string_equals_query()
        {
            var i = F("$filter=data/firstName/de eq 'Sebastian'");
            var o = C("{ 'Data.1.de' : 'Sebastian' }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_create_string_not_equals_query()
        {
            var i = F("$filter=data/firstName/de ne 'Sebastian'");
            var o = C("{ 'Data.1.de' : { '$ne' : 'Sebastian' } }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_create_number_less_than_query()
        {
            var i = F("$filter=data/age/iv lt 1");
            var o = C("{ 'Data.4.iv' : { '$lt' : 1.0 } }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_create_number_less_equals_query()
        {
            var i = F("$filter=data/age/iv le 1");
            var o = C("{ 'Data.4.iv' : { '$lte' : 1.0 } }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_create_number_greater_than_query()
        {
            var i = F("$filter=data/age/iv gt 1");
            var o = C("{ 'Data.4.iv' : { '$gt' : 1.0 } }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_create_number_greater_equals_query()
        {
            var i = F("$filter=data/age/iv ge 1");
            var o = C("{ 'Data.4.iv' : { '$gte' : 1.0 } }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_create_and_query()
        {
            var i = F("$filter=data/age/iv eq 1 and data/age/iv eq 2");
            var o = C("{ '$and' : [{ 'Data.4.iv' : 1.0 }, { 'Data.4.iv' : 2.0 }] }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_create_or_query()
        {
            var i = F("$filter=data/age/iv eq 1 or data/age/iv eq 2");
            var o = C("{ '$or' : [{ 'Data.4.iv' : 1.0 }, { 'Data.4.iv' : 2.0 }] }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_create_full_text_query()
        {
            var i = F("$search=Hello my World");
            var o = C("{ '$text' : { '$search' : 'Hello my World' } }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_create_full_text_query_with_and()
        {
            var i = F("$search=A and B");
            var o = C("{ '$text' : { '$search' : 'A and B' } }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_convert_orderby_with_single_statements()
        {
            var i = S("$orderby=data/age/iv desc");
            var o = C("{ 'Data.4.iv' : -1 }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_convert_orderby_with_multiple_statements()
        {
            var i = S("$orderby=data/age/iv, data/firstName/en desc");
            var o = C("{ 'Data.4.iv' : 1, 'Data.1.en' : -1 }");

            Assert.Equal(o, i);
        }

        [Fact]
        public void Should_set_top()
        {
            var parser = schema.ParseQuery(languages, "$top=3");
            var cursor = new Mock<IFindFluent<MongoContentEntity, MongoContentEntity>>();

            cursor.Object.Take(parser);

            cursor.Verify(x => x.Limit(3));
        }

        [Fact]
        public void Should_not_set_top()
        {
            var parser = schema.ParseQuery(languages, "");
            var cursor = new Mock<IFindFluent<MongoContentEntity, MongoContentEntity>>();

            cursor.Object.Take(parser);

            cursor.Verify(x => x.Limit(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void Should_set_skip()
        {
            var parser = schema.ParseQuery(languages, "$skip=3");
            var cursor = new Mock<IFindFluent<MongoContentEntity, MongoContentEntity>>();

            cursor.Object.Skip(parser);

            cursor.Verify(x => x.Skip(3));
        }

        [Fact]
        public void Should_not_set_skip()
        {
            var parser = schema.ParseQuery(languages, "");
            var cursor = new Mock<IFindFluent<MongoContentEntity, MongoContentEntity>>();

            cursor.Object.Take(parser);

            cursor.Verify(x => x.Skip(It.IsAny<int>()), Times.Never);
        }

        private static string C(string value)
        {
            return value.Replace('\'', '"');
        }

        private string S(string value)
        {
            var parser = schema.ParseQuery(languages, value);
            var cursor = new Mock<IFindFluent<MongoContentEntity, MongoContentEntity>>();

            var i = string.Empty;

            cursor.Setup(x => x.Sort(It.IsAny<SortDefinition<MongoContentEntity>>())).Callback(new Action<SortDefinition<MongoContentEntity>>(s =>
            {
                i = s.Render(serializer, registry).ToString();
            }));

            cursor.Object.Sort(parser, schema);

            return i;
        }

        private string F(string value)
        {
            var parser = schema.ParseQuery(languages, value);

            var query = FilterBuilder.Build(parser, schema).Render(serializer, registry).ToString();

            return query;
        }
    }
}
