import { Component, Inject, OnDestroy, OnInit } from '@angular/core';
import { DOCUMENT } from '@angular/common';
// Import the AuthService type from the SDK
import { AuthService } from '@auth0/auth0-angular';
import { User, UserData } from 'app/@core/data/users';
import { Subject } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';
import { NbMediaBreakpointsService, NbMenuService, NbThemeService } from '@nebular/theme';
import { map } from 'rxjs/operators';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { HubHttpService } from '../hub-http-service';

@Component({
  selector: 'app-auth-button',
  template: `
  <ng-container *ngIf="auth.isAuthenticated$ | async; else loggedOut">
  <nb-user [nbContextMenu]="userMenu"
               [onlyPicture]="userPictureOnly"
               [name]="user?.name"
               [picture]="user?.picture"
               nbContextMenuTag="user-context-menu">
      </nb-user>
      <!-- <button nbButton status="primary" outline (click)="auth.logout({ returnTo: document.location.origin })">
        Log out
      </button> -->
    </ng-container>

    <ng-template #loggedOut>
      <button nbButton status="primary" outline (click)="auth.loginWithRedirect()">Log in</button>
    </ng-template>`
})
export class AuthButtonComponent implements OnInit, OnDestroy {
  // Inject the authentication service into your component through the constructor
  constructor(@Inject(DOCUMENT) public document: Document, public auth: AuthService,
    private themeService: NbThemeService, private httpService: HubHttpService,
    private http: HttpClient,
    private breakpointService: NbMediaBreakpointsService, private menuService: NbMenuService) { }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private destroy$: Subject<void> = new Subject<void>();
  userPictureOnly: boolean = false;
  user: User;

  userMenu = [{ title: 'Log out' }];

  ngOnInit(): void {

    const { xl } = this.breakpointService.getBreakpointsMap();
    this.themeService.onMediaQueryChange()
      .pipe(
        map(([, currentBreakpoint]) => currentBreakpoint.width < xl),
        takeUntil(this.destroy$),
      )
      .subscribe((isLessThanXl: boolean) => this.userPictureOnly = isLessThanXl);

    this.menuService.onItemClick()
      .pipe(
        filter(({ tag }) => tag === 'user-context-menu'),
        map(({ item: { title } }) => title),
      )
      .subscribe(title => {
        if (title == 'Log out') {
          this.auth.logout({ returnTo: document.location.origin });
        }
      });

      this.auth.user$.subscribe(usr => {
        if (usr == null)
        return;
        this.user = {
          name: usr.name,
          picture: null
        };
      });
  }
}