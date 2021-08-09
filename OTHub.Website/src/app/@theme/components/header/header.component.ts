import { Component, ElementRef, OnDestroy, OnInit, Renderer2, ViewChild } from '@angular/core';
import { NbMediaBreakpointsService, NbMenuService, NbPopoverDirective, NbSidebarService, NbThemeService, NbToastrConfig, NbToastrService } from '@nebular/theme';
import * as signalR from "@microsoft/signalr";
import { UserData } from '../../../@core/data/users';
import { LayoutService } from '../../../@core/utils';
import { filter, map, takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { AuthService } from '@auth0/auth0-angular';
import { HubHttpService } from 'app/pages/hub-http-service';
import { HttpClient, HttpHeaders } from '@angular/common/http';

import * as confetti from 'canvas-confetti';

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
    private http: HttpClient,
    private toastrService: NbToastrService) {
    this.isDisconnected = true;
    this.isConnected = false;
    this.hasLoadingSignalrStarted = false;
    this.hasEverConnected = false;
    this.hasNewNotifications = false;
    this.isNotificationsAreaOpen = false;
  }


  
  randomInRange(min, max) {
    return Math.random() * (max - min) + min;
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

  fireConfetti() {
    const that = this;

    const canvas = document.getElementById('confettiCanvas');
    canvas.style.display = 'block';

    const myConfetti = confetti.create(canvas, {
      resize: true
    });

    const duration = 8 * 1000;
    const animationEnd = Date.now() + duration;
    const defaults = { startVelocity: 35, spread: 360, ticks: 60, zIndex: 0 };

    let interval = setInterval(function () {
      let timeLeft = animationEnd - Date.now();

      if (timeLeft <= 0) {
        canvas.style.display = 'none';
        return clearInterval(interval);
      }

      let particleCount = 75 * (timeLeft / duration);
      // since particles fall down, start a bit higher than random
      myConfetti(Object.assign({}, defaults, { particleCount, origin: { x: that.randomInRange(0.1, 0.4), y: Math.random() - 0.2 } }));
      myConfetti(Object.assign({}, defaults, { particleCount, origin: { x: that.randomInRange(0.6, 0.9), y: Math.random() - 0.2 } }));
    }, 450);

    const colors = ['#16b804', '#ffffff'];

    (function frame() {
      myConfetti({
        particleCount: 2,
        angle: 60,
        spread: 55,
        origin: { x: 0 },
        colors: colors
      });
      myConfetti({
        particleCount: 2,
        angle: 120,
        spread: 55,
        origin: { x: 1 },
        colors: colors
      });
    
      if (Date.now() < animationEnd) {
        requestAnimationFrame(frame);
      }
    }());
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

          this.connection.on('JobWon', (title, description) => {
            this.jobWonFired(title, description);
          });

          this.tryConnect();
        });
      }
    });
  }

  jobWonFired(title, description) {
    this.getNotifications(false).subscribe(data => {
      this.processNotifications(data);
    });
    this.fireConfetti();
    let config = new NbToastrConfig({ duration: 8000 });
    config.status = "success";
    config.icon = 'checkmark-circle-2-outline';
    this.toastrService.show(
      description, title, config);
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
