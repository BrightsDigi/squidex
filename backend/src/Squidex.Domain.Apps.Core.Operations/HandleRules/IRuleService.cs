﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Squidex.Domain.Apps.Events;
using Squidex.Infrastructure.EventSourcing;

namespace Squidex.Domain.Apps.Core.HandleRules
{
    public interface IRuleService
    {
        bool CanCreateSnapshotEvents(RuleContext context);

        string GetName(AppEvent @event);

        IAsyncEnumerable<CreatedJob> CreateSnapshotJobsAsync(RuleContext context, CancellationToken ct = default);

        IAsyncEnumerable<CreatedJob> CreateJobsAsync(Envelope<IEvent> @event, RuleContext context, CancellationToken ct = default);

        Task<(Result Result, TimeSpan Elapsed)> InvokeAsync(string actionName, string job);
    }
}