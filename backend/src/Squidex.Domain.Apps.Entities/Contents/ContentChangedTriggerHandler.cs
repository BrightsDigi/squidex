﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Squidex.Domain.Apps.Core.Contents;
using Squidex.Domain.Apps.Core.HandleRules;
using Squidex.Domain.Apps.Core.Rules.EnrichedEvents;
using Squidex.Domain.Apps.Core.Rules.Triggers;
using Squidex.Domain.Apps.Core.Scripting;
using Squidex.Domain.Apps.Entities.Contents.Repositories;
using Squidex.Domain.Apps.Events;
using Squidex.Domain.Apps.Events.Contents;
using Squidex.Infrastructure;
using Squidex.Infrastructure.EventSourcing;
using Squidex.Infrastructure.Reflection;
using Squidex.Text;

namespace Squidex.Domain.Apps.Entities.Contents
{
    public sealed class ContentChangedTriggerHandler : IRuleTriggerHandler
    {
        private readonly IScriptEngine scriptEngine;
        private readonly IContentLoader contentLoader;
        private readonly IContentRepository contentRepository;

        public bool CanCreateSnapshotEvents => true;

        public Type TriggerType => typeof(ContentChangedTriggerV2);

        public ContentChangedTriggerHandler(
            IScriptEngine scriptEngine,
            IContentLoader contentLoader,
            IContentRepository contentRepository)
        {
            Guard.NotNull(scriptEngine, nameof(scriptEngine));
            Guard.NotNull(contentLoader, nameof(contentLoader));
            Guard.NotNull(contentRepository, nameof(contentRepository));

            this.scriptEngine = scriptEngine;
            this.contentLoader = contentLoader;
            this.contentRepository = contentRepository;
        }

        public async IAsyncEnumerable<EnrichedEvent> CreateSnapshotEventsAsync(RuleContext context,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (context.Rule.Trigger is not ContentChangedTriggerV2 trigger)
            {
                yield break;
            }

            var schemaIds =
                trigger.Schemas?.Count > 0 ?
                trigger.Schemas.Select(x => x.SchemaId).Distinct().ToHashSet() :
                null;

            await foreach (var content in contentRepository.StreamAll(context.AppId.Id, schemaIds, ct))
            {
                var result = new EnrichedContentEvent
                {
                    Type = EnrichedContentEventType.Created
                };

                SimpleMapper.Map(content, result);

                result.Actor = content.LastModifiedBy;
                result.Name = $"ContentQueried({content.SchemaId.Name.ToPascalCase()})";

                yield return result;
            }
        }

        public async IAsyncEnumerable<EnrichedEvent> CreateEnrichedEventsAsync(Envelope<AppEvent> @event, RuleContext context,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (@event.Payload is not ContentEvent contentEvent)
            {
                yield break;
            }

            var result = new EnrichedContentEvent();

            var content =
                await contentLoader.GetAsync(
                    contentEvent.AppId.Id,
                    contentEvent.ContentId,
                    @event.Headers.EventStreamNumber());

            if (content != null)
            {
                SimpleMapper.Map(content, result);
            }

            switch (@event.Payload)
            {
                case ContentCreated:
                    result.Type = EnrichedContentEventType.Created;
                    break;
                case ContentDeleted:
                    result.Type = EnrichedContentEventType.Deleted;
                    break;

                case ContentStatusChanged statusChanged:
                    {
                        switch (statusChanged.Change)
                        {
                            case StatusChange.Published:
                                result.Type = EnrichedContentEventType.Published;
                                break;
                            case StatusChange.Unpublished:
                                result.Type = EnrichedContentEventType.Unpublished;
                                break;
                            default:
                                result.Type = EnrichedContentEventType.StatusChanged;
                                break;
                        }

                        break;
                    }

                case ContentUpdated:
                    {
                        result.Type = EnrichedContentEventType.Updated;

                        if (content != null)
                        {
                            var previousContent =
                                await contentLoader.GetAsync(
                                    content.AppId.Id,
                                    content.Id,
                                    content.Version - 1);

                            if (previousContent != null)
                            {
                                result.DataOld = previousContent.Data;
                            }
                        }

                        break;
                    }
            }

            yield return result;
        }

        public string? GetName(AppEvent @event)
        {
            if (@event is ContentEvent contentEvent)
            {
                return $"{@event.GetType().Name}({contentEvent.SchemaId.Name.ToPascalCase()})";
            }

            return null;
        }

        public bool Trigger(Envelope<AppEvent> @event, RuleContext context)
        {
            if (context.Rule.Trigger is not ContentChangedTriggerV2 trigger)
            {
                return false;
            }

            if (@event.Payload is not ContentEvent)
            {
                return false;
            }

            if (trigger.HandleAll)
            {
                return true;
            }

            if (trigger.Schemas != null)
            {
                var contentEvent = (ContentEvent)@event.Payload;

                foreach (var schema in trigger.Schemas)
                {
                    if (MatchsSchema(schema, contentEvent.SchemaId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool Trigger(EnrichedEvent @event, RuleContext context)
        {
            if (context.Rule.Trigger is not ContentChangedTriggerV2 trigger)
            {
                return false;
            }

            if (@event is not EnrichedContentEvent)
            {
                return false;
            }

            if (trigger.HandleAll)
            {
                return true;
            }

            if (trigger.Schemas != null)
            {
                var contentEvent = (EnrichedContentEvent)@event;

                foreach (var schema in trigger.Schemas)
                {
                    if (MatchsSchema(schema, contentEvent.SchemaId) && MatchsCondition(schema, contentEvent))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool MatchsSchema(ContentChangedTriggerSchemaV2? schema, NamedId<DomainId> eventId)
        {
            return eventId.Id == schema?.SchemaId;
        }

        private bool MatchsCondition(ContentChangedTriggerSchemaV2 schema, EnrichedSchemaEventBase @event)
        {
            if (string.IsNullOrWhiteSpace(schema.Condition))
            {
                return true;
            }

            var vars = new ScriptVars
            {
                ["event"] = @event
            };

            return scriptEngine.Evaluate(vars, schema.Condition);
        }
    }
}
