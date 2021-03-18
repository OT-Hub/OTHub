/**
 * @license
 * Copyright Akveo. All Rights Reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';

import {Ng2SmartTableModule} from "ng2-smart-table";
import { AuthModule } from '@auth0/auth0-angular';

@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    AppRoutingModule,
    Ng2SmartTableModule,
    AuthModule.forRoot({
      domain: 'othub.eu.auth0.com',
      clientId: 'Yx384WexDQj9xz8DBK62mdUw74G54f2B'
    }),
  ],
  bootstrap: [AppComponent],
})
export class AppModule {
  constructor() {

  }
}
