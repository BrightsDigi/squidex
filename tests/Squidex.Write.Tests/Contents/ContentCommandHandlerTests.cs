﻿// ==========================================================================
//  ContentCommandHandlerTests.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System;
using System.Threading.Tasks;
using Moq;
using Squidex.Core.Contents;
using Squidex.Core.Schemas;
using Squidex.Infrastructure;
using Squidex.Infrastructure.CQRS.Commands;
using Squidex.Read.Apps;
using Squidex.Read.Apps.Services;
using Squidex.Read.Schemas;
using Squidex.Read.Schemas.Services;
using Squidex.Write.Contents.Commands;
using Squidex.Write.Utils;
using Xunit;
// ReSharper disable ConvertToConstant.Local

namespace Squidex.Write.Contents
{
    public class ContentCommandHandlerTests : HandlerTestBase<ContentDomainObject>
    {
        private readonly ContentCommandHandler sut;
        private readonly ContentDomainObject content;
        private readonly Mock<ISchemaProvider> schemaProvider = new Mock<ISchemaProvider>();
        private readonly Mock<IAppProvider> appProvider = new Mock<IAppProvider>();
        private readonly Mock<ISchemaEntityWithSchema> schemaEntity = new Mock<ISchemaEntityWithSchema>();
        private readonly Mock<IAppEntity> appEntity = new Mock<IAppEntity>();
        private readonly Guid schemaId = Guid.NewGuid();
        private readonly Guid appId = Guid.NewGuid();
        private readonly ContentData data = new ContentData().AddField("my-field", new ContentFieldData().SetValue(1));

        public ContentCommandHandlerTests()
        {
            var schema = 
                Schema.Create("my-schema", new SchemaProperties())
                    .AddOrUpdateField(new NumberField(1, "my-field", 
                        new NumberFieldProperties { IsRequired = true }));

            content = new ContentDomainObject(Id, 0);

            sut = new ContentCommandHandler(Handler, appProvider.Object, schemaProvider.Object);

            appEntity.Setup(x => x.Languages).Returns(new[] { Language.DE });
            appProvider.Setup(x => x.FindAppByIdAsync(appId)).Returns(Task.FromResult(appEntity.Object));

            schemaEntity.Setup(x => x.Schema).Returns(schema);
            schemaProvider.Setup(x => x.FindSchemaByIdAsync(schemaId)).Returns(Task.FromResult(schemaEntity.Object));
        }

        [Fact]
        public async Task Create_should_throw_exception_if_data_is_not_valid()
        {
            var command = new CreateContent { AggregateId = Id, AppId = appId, SchemaId = schemaId, Data = new ContentData() };
            var context = new CommandContext(command);

            await TestCreate(content, async _ =>
            {
                await Assert.ThrowsAsync<ValidationException>(() => sut.HandleAsync(context));
            }, false);
        }

        [Fact]
        public async Task Create_should_create_content()
        {
            var command = new CreateContent { AggregateId = Id, AppId = appId, SchemaId = schemaId, Data = data };
            var context = new CommandContext(command);

            await TestCreate(content, async _ =>
            {
                await sut.HandleAsync(context);
            });

            Assert.Equal(Id, context.Result<Guid>());
        }

        [Fact]
        public async Task Update_should_throw_exception_if_data_is_not_valid()
        {
            CreateContent();

            var command = new UpdateContent { AggregateId = Id, AppId = appId, SchemaId = schemaId, Data = new ContentData() };
            var context = new CommandContext(command);

            await TestUpdate(content, async _ =>
            {
                await Assert.ThrowsAsync<ValidationException>(() => sut.HandleAsync(context));
            }, false);
        }

        [Fact]
        public async Task Update_should_update_domain_object()
        {
            CreateContent();

            var command = new UpdateContent { AggregateId = Id, AppId = appId, SchemaId = schemaId, Data = data };
            var context = new CommandContext(command);

            await TestUpdate(content, async _ =>
            {
                await sut.HandleAsync(context);
            });
        }

        [Fact]
        public async Task Patch_should_throw_exception_if_data_is_not_valid()
        {
            CreateContent();

            var command = new PatchContent { AggregateId = Id, AppId = appId, SchemaId = schemaId, Data = new ContentData() };
            var context = new CommandContext(command);

            await TestUpdate(content, async _ =>
            {
                await Assert.ThrowsAsync<ValidationException>(() => sut.HandleAsync(context));
            }, false);
        }

        [Fact]
        public async Task Patch_should_update_domain_object()
        {
            CreateContent();

            var command = new PatchContent { AggregateId = Id, AppId = appId, SchemaId = schemaId, Data = data };
            var context = new CommandContext(command);

            await TestUpdate(content, async _ =>
            {
                await sut.HandleAsync(context);
            });
        }

        [Fact]
        public async Task Publish_should_publish_domain_object()
        {
            CreateContent();

            var command = new PublishContent { AggregateId = Id, AppId = appId, SchemaId = schemaId };
            var context = new CommandContext(command);

            await TestUpdate(content, async _ =>
            {
                await sut.HandleAsync(context);
            });
        }

        [Fact]
        public async Task Unpublish_should_unpublish_domain_object()
        {
            CreateContent();

            var command = new UnpublishContent { AggregateId = Id, AppId = appId, SchemaId = schemaId };
            var context = new CommandContext(command);

            await TestUpdate(content, async _ =>
            {
                await sut.HandleAsync(context);
            });
        }

        [Fact]
        public async Task Delete_should_update_domain_object()
        {
            CreateContent();

            var command = new DeleteContent { AggregateId = Id };
            var context = new CommandContext(command);

            await TestUpdate(content, async _ =>
            {
                await sut.HandleAsync(context);
            });
        }

        private void CreateContent()
        {
            content.Create(new CreateContent { Data = data });
        }
    }
}
