import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NbMediaBreakpointsService, NbMenuService, NbPopoverDirective, NbSidebarService, NbThemeService } from '@nebular/theme';
import * as signalR from "@microsoft/signalr";
import { UserData } from '../../../@core/data/users';
import { LayoutService } from '../../../@core/utils';
import { filter, map, takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { AuthService } from '@auth0/auth0-angular';
import { HubHttpService } from 'app/pages/hub-http-service';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Component({
  selector: 'ngx-header',
  styleUrls: ['./header.component.scss'],
  templateUrl: './header.component.html',
})
export class HeaderComponent implements OnInit, OnDestroy {

  private destroy$: Subject<void> = new Subject<void>();

  connection: signalR.HubConnection;

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
      name: 'Mainnet',
      url: 'https://othub.origin-trail.network'
    },
    {
      name: 'Testnet (ethr:rinkeby:1)',
      url: 'https://othub-testnet.origin-trail.network'
    }
  ];

  currentNetwork = 'Mainnet';
  currentTheme = 'default';
  notifications: NotificationModel[];

  constructor(private sidebarService: NbSidebarService,
    private menuService: NbMenuService,
    private themeService: NbThemeService,
    private layoutService: LayoutService,
    private auth: AuthService,
    private httpService: HubHttpService,
    private http: HttpClient) {
    this.isDisconnected = true;
    this.isConnected = false;
    this.hasLoadingSignalrStarted = false;
    this.hasEverConnected = false;
    this.hasNewNotifications = false;
    this.isNotificationsAreaOpen = false;
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

  isDisconnected: boolean;
  isConnected: boolean;
  hasEverConnected: boolean;
  hasLoadingSignalrStarted: boolean;
  hasNewNotifications: boolean;
  isNotificationsAreaOpen: boolean;

  @ViewChild(NbPopoverDirective) popover: NbPopoverDirective;

  ngAfterViewInit() {

    this.popover.nbPopoverShowStateChange.subscribe(popoverState => {
      this.isNotificationsAreaOpen = popoverState.isShown;
      if (popoverState.isShown) {
        this.getNotifications(true).subscribe(data => {
          this.processNotifications(data);
        });
      }
    });


    this.auth.user$.subscribe(usr => {
      if (usr != null && this.hasLoadingSignalrStarted == false) {
        this.hasLoadingSignalrStarted = true;

        this.getNotifications(false).subscribe(data => {
          this.processNotifications(data);
        });

        this.auth.getAccessTokenSilently().subscribe(token => {
          let url = this.httpService.ApiUrl + '/signalr/notifications';

          Object.defineProperty(window.WebSocket, 'OPEN', { value: 1, });

          this.connection = new signalR.HubConnectionBuilder()
            .withUrl(url, {
              transport: signalR.HttpTransportType.WebSockets,
              accessTokenFactory: () => token
            })
            .withAutomaticReconnect()
            .build();



          this.connection.onreconnecting(data => {
            this.isConnected = false;
            this.isDisconnected = true;
          });

          this.connection.onreconnected(data => {
            this.isDisconnected = false;
            this.isConnected = true;
          });

          this.connection.on('JobWon', (data) => {
            this.getNotifications(false).subscribe(data => {
              this.processNotifications(data);
            });
          });

          this.tryConnect();
        });
      }
    });
  }

  tryConnect() {
    this.connection.start().then(() => {
      this.isDisconnected = false;
      this.isConnected = true;
      this.hasEverConnected = true;
    }).catch(err => {
      this.isConnected = false;
      this.isDisconnected = true;
      if (this.hasEverConnected == false) {
        setTimeout(() => this.tryConnect(), 20000);
      }
    });
  }

  getNotifications(markOldNotificationsAsRead: boolean) {

    if (markOldNotificationsAsRead == true) {
      if (this.notifications != null && this.notifications.length > 0) {
        const latest = this.notifications[0];
        this.markNotificationsAsRead(latest.Date);
      }
    }

    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/notifications' + '?' + (new Date()).getTime();
    const promise = this.http.get<NotificationModel[]>(url, { headers: headers });
    return promise;
  }

  processNotifications(notifications: NotificationModel[]) {
    this.notifications = notifications;

    let anyUnread = false;

    notifications.forEach(n => {
      if (!n.Read) {
        anyUnread = true;
        return;
      }
    });

    

    if (this.isNotificationsAreaOpen == true) {
      if (this.notifications != null && this.notifications.length > 0) {
        const latest = this.notifications[0];
        this.markNotificationsAsRead(latest.Date);
      }
      this.hasNewNotifications = false;
    } else {
      this.hasNewNotifications = anyUnread;
    }
    
  }

  lastSentNotificationReadDate: Date;
  markNotificationsAsRead(upToDate: Date) {

    if (this.lastSentNotificationReadDate == upToDate)
    return;

    this.lastSentNotificationReadDate = upToDate;

    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/notifications/MarkAsRead' + '?upToDate=' + upToDate;
    const promise = this.http.post(url, { headers: headers }).subscribe(d => {
     });
  }

  dismissNotifications() {
    let upToDate;
    if (this.notifications != null && this.notifications.length > 0) {
      const latest = this.notifications[0];
      upToDate = latest.Date;
    } else {
      return;
    }

    this.notifications = [];

    const headers = new HttpHeaders()
    .set('Content-Type', 'application/json')
    .set('Accept', 'application/json');
  const url = this.httpService.ApiUrl + '/api/notifications/Dismiss' + '?upToDate=' + upToDate;
  const promise = this.http.post(url, { headers: headers }).subscribe(d => {
   });
  }
}

export interface NotificationModel {
  Title: string;
  Date: Date;
  Description: string;
  Url: string;
  Read: boolean;
}
