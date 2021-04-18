import { Component, OnDestroy, OnInit } from '@angular/core';
import { NbMediaBreakpointsService, NbMenuService, NbSidebarService, NbThemeService } from '@nebular/theme';

import { UserData } from '../../../@core/data/users';
import { LayoutService } from '../../../@core/utils';
import { filter, map, takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';

@Component({
  selector: 'ngx-header',
  styleUrls: ['./header.component.scss'],
  templateUrl: './header.component.html',
})
export class HeaderComponent implements OnInit, OnDestroy {

  private destroy$: Subject<void> = new Subject<void>();


  themes = [
    {
      value: 'default',
      name: 'Light',
    },
    {
      value: 'dark',
      name: 'Dark',
    },
    {
      value: 'cosmic',
      name: 'Cosmic',
    },
    {
      value: 'corporate',
      name: 'Corporate',
    },
  ];

  networks = [
    {
      name: 'Mainnet (V5)',
      url: 'https://v5.othub.info'
    },
    {
      name: 'Mainnet (Legacy)',
      url: 'https://othub.origin-trail.network'
    },
    {
      name: 'Testnet',
      url: 'https://othub-testnet.origin-trail.network'
    }
  ];

  currentNetwork = 'Mainnet';
  currentTheme = 'default';



  constructor(private sidebarService: NbSidebarService,
    private menuService: NbMenuService,
    private themeService: NbThemeService,
    private layoutService: LayoutService) {
  }

  ngOnInit() {
    this.currentTheme = this.themeService.currentTheme;

    if (location.hostname === "localhost") {
      const url = 'http://' + location.host;
      this.networks.push({
        name: 'Localhost',
        url: url
      });

      this.currentNetwork = url;
    } else {
      for (let i = 0; i < this.networks.length; i++) {
        const network = this.networks[i];

        if (network.url.includes(location.hostname)) {
          this.currentNetwork = network.url;
          break;
        }
      }
    }




    this.themeService.onThemeChange()
      .pipe(
        map(({ name }) => name),
        takeUntil(this.destroy$),
      )
      .subscribe(themeName => this.currentTheme = themeName);


  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  changeTheme(themeName: string) {
    this.themeService.changeTheme(themeName);
  }

  changeNetwork(networkName: string) {
    if (!window.location.href.includes(networkName)) {
      window.location.href = networkName;
    }
  }

  toggleSidebar(): boolean {
    this.sidebarService.toggle(true, 'menu-sidebar');
    this.layoutService.changeLayoutSize();

    return false;
  }

  navigateHome() {
    this.menuService.navigateHome();
    return false;
  }
}
