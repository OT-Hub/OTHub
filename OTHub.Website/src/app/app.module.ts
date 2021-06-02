/**
 * @license
 * Copyright Akveo. All Rights Reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';
import { CoreModule } from './@core/core.module';
import { ThemeModule } from './@theme/theme.module';
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import {
  NbDialogModule,
  NbMenuModule,
  NbSidebarModule,
  NbToastrModule,
  NbWindowModule,
  NbThemeService, NbCardModule, NbSelectModule, NbDatepickerModule
} from '@nebular/theme';
import {Ng2SmartTableModule} from "ng2-smart-table";
import { AuthModule } from '@auth0/auth0-angular';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthHttpInterceptor } from '@auth0/auth0-angular';
@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    AppRoutingModule,
    NbSidebarModule.forRoot(),
    NbMenuModule.forRoot(),
    NbDialogModule.forRoot(),
    NbWindowModule.forRoot(),
    NbToastrModule.forRoot(),
    CoreModule.forRoot(),
    ThemeModule.forRoot(),
    NbDatepickerModule.forRoot(),
    Ng2SmartTableModule,
    NbCardModule,
    NbSelectModule,
    AuthModule.forRoot({
      domain: 'othub.eu.auth0.com',
      clientId: 'Yx384WexDQj9xz8DBK62mdUw74G54f2B',
      scope: 'profile offline_access openid',
      audience: 'https://othubapi',
      allowAnonymous: true,
      httpInterceptor: {
        allowedList: [
          {
            uri: `http://localhost:5000/*`,
            allowAnonymous: true,
          }, {
            uri: `https://v5api.othub.info/*`,
            allowAnonymous: true,
          }, {
            uri: `https://testnet-api.othub.info/*`,
            allowAnonymous: true,
          }]
      },
    }),
  ],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthHttpInterceptor,
      multi: true,
    },
  ],
  bootstrap: [AppComponent],
})
export class AppModule {
  constructor(private themeService: NbThemeService) {

    this.themeService.onThemeChange()
          .subscribe((theme: any) => {
            localStorage.setItem('theme', theme.name);
          });
  }
}
