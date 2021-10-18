import { Component, Input, OnInit, EventEmitter, Output, HostListener } from '@angular/core';
import * as moment from 'moment';
import { ViewCell } from 'ng2-smart-table';
import { NbIconLibraries, NbIconComponent, NbComponentStatus } from '@nebular/theme';
import { RouterModule } from '@angular/router';
import { HubHttpService } from '../hub-http-service';

@Component({
    selector: 'offeridcolumn',
    template: `
    <a [routerLink]="[link]" [title]="value">
    {{value | slice:0:6}}...{{value | slice:-6}}
    </a>
  `,
})
export class OfferIdColumnComponent implements ViewCell, OnInit {

    renderValue: string;

    @Input() value: string;
    @Input() rowData: any;

    link: string;

    constructor(private httpService: HubHttpService) {

    }

    ngOnInit() {
        this.renderValue = this.value;
        this.link = '/offers/' + this.value;
    }
}