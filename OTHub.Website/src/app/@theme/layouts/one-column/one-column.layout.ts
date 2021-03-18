import { Component, ViewChild, ElementRef } from '@angular/core';
import { NbSidebarComponent, NbSidebarService } from '@nebular/theme';
import { Router, NavigationStart } from '@angular/router';

@Component({
  selector: 'ngx-one-column-layout',
  styleUrls: ['./one-column.layout.scss'],
  template: `
    <nb-layout>
      <nb-layout-header fixed>
        <ngx-header></ngx-header>
      </nb-layout-header>

      <nb-sidebar class="menu-sidebar" tag="menu-sidebar" responsive #menuSidebar>
        <ng-content select="nb-menu"></ng-content>
      </nb-sidebar>

      <nb-layout-column>
        <ng-content select="router-outlet"></ng-content>
      </nb-layout-column>

      <nb-layout-footer fixed>
        <ngx-footer></ngx-footer>
      </nb-layout-footer>
    </nb-layout>
  `,
})
export class OneColumnLayoutComponent {
  @ViewChild('menuSidebar') menuSidebar: any;

  constructor(private sidebarService: NbSidebarService, private router: Router) {
    this.isFixed = false;

    this.router.events.subscribe(val => {
      if (val instanceof NavigationStart && this.isFixed == true) {
        this.sidebarService.collapse('menu-sidebar');
      }
    });
  }

  isFixed: boolean;

  ngDoCheck() {

    if (this.menuSidebar && this.menuSidebar.element.nativeElement) {
      if (this.menuSidebar.element.nativeElement.classList.contains('fixed')) {
        this.isFixed = true;
      } else {
        this.isFixed = false;
      }
    }
  }
}
