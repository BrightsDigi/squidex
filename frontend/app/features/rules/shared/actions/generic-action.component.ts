/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */

import { Component, Input } from '@angular/core';
import { ActionForm } from '@app/shared';

@Component({
    selector: 'sqx-generic-action',
    styleUrls: ['./generic-action.component.scss'],
    templateUrl: './generic-action.component.html'
})
export class GenericActionComponent {
    @Input()
    public actionForm: ActionForm;
}