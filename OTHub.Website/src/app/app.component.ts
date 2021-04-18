/**
 * @license
 * Copyright Akveo. All Rights Reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 */
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';
import { HubHttpService } from './pages/hub-http-service';

@Component({
  selector: 'ngx-app',
  template: '<router-outlet></router-outlet>',
})
export class AppComponent implements OnInit {

hasSentUserRequest: boolean;

  constructor(
    public auth: AuthService, private httpService: HubHttpService,
    private http: HttpClient,) {
      this.hasSentUserRequest = false;
  }

  ngOnInit(): void {
    this.auth.user$.subscribe(usr => {
      if (this.hasSentUserRequest || usr == null) {
        return;
      }
      this.hasSentUserRequest = true;
      const headers = new HttpHeaders()
        .set('Content-Type', 'application/json')
        .set('Accept', 'application/json');
      const url = this.httpService.ApiUrl + '/api/user/EnsureCreated';
      this.http.post(url, { headers }).subscribe(data => {
      });
    });
  }
}
