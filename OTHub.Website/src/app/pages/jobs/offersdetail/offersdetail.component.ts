import { Component, OnInit } from '@angular/core';
import {
  HttpClient,
  HttpHeaders
} from '@angular/common/http';
import { MomentModule } from 'ngx-moment';
import { OTOfferDetailModel, OTOfferDetailTimelineModel } from './offersdetail-models';
import { ActivatedRoute, Router } from '@angular/router';
import { MyNodeService } from '../../nodes/mynodeservice';
import { HubHttpService } from '../../hub-http-service';
import Web3 from 'web3';
declare const $: any;
@Component({
  selector: 'app-offersdetail',
  templateUrl: './offersdetail.component.html',
  styleUrls: ['./offersdetail.component.scss']
})
export class OffersDetailComponent implements OnInit {
  web3: any;
  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router, private myNodeService: MyNodeService,
              private httpService: HubHttpService) {
    this.isLoading = true;
    this.failedLoading = false;
    this.web3 = new Web3();
  }

  OfferModel: OTOfferDetailModel;
  offerId: string;
  isLoading: boolean;
  failedLoading: boolean;
  isDarkTheme: boolean;

  formatAmount(amount) {
    if (amount === null) {
      return null;
    }
    const split = amount.toString().split('.');
    let lastSplit = '';
    if (split.length === 2) {
      lastSplit = split[1];
      if (lastSplit.length > 3) {
        lastSplit = lastSplit.substring(0, 3);
      }
      return split[0] + '.' + lastSplit;
    }
    return split[0];
  }

  getTimelineItemColour(timeline: OTOfferDetailTimelineModel) {
    if (timeline.Name === 'Offer Created') {
      return '#3949ab';
    } else if (timeline.Name === 'Offer Finalized') {
      return '#3949ab';
    } else if (timeline.Name === 'Offer Completed') {
      return '#3949ab';
    } else if (timeline.Name === 'Data Holder Replaced') {
      return '#c62828';
    } else if (timeline.Name === 'Data Holder Chosen (Replacement)') {
      return '#3949ab';
    } else if (timeline.Name === 'Litigation Initiated') {
      return '#f9a825';
    } else if (timeline.Name === 'Litigation Timed Out') {
      return '#c62828';
    } else if (timeline.Name === 'Litigation Answered') {
      return '#f9a825';
    } else if (timeline.Name === 'Litigation Failed') {
      return '#c62828';
    } else if (timeline.Name === 'Litigation Passed') {
      return '#689f38';
    } else if (timeline.Name.startsWith('Offer Paidout')) {
      return '#689f38';
    } else if (timeline.Name === 'Data Holder Chosen') {
      return '#3949ab';
    }
    return '#666';
  }

  toFixed(num, fixed) {
    const re = new RegExp('^-?\\d+(?:\.\\d{0,' + (fixed || -1) + '})?');
    return num.toString().match(re)[0];
  }

  getIdentityIcon(identity: string) {
    return this.httpService.ApiUrl + '/api/icon/node/' + identity + '/' + (this.isDarkTheme ? 'dark' : 'light') + '/16';
  }

  getTimelineItemIcon(timeline: OTOfferDetailTimelineModel) {
    if (timeline.Name === 'Offer Created') {
      return 'far fa-calendar';
    } else if (timeline.Name === 'Offer Finalized') {
      return 'far fa-calendar-plus';
    } else if (timeline.Name === 'Offer Completed') {
      return 'far fa-calendar-check';
    } else if (timeline.Name === 'Replacement Started') {
      return 'fas fa-bomb';
    } else if (timeline.Name === 'Data Holder Replaced') {
      return 'fas fa-bomb';
    } else if (timeline.Name === 'Litigation Initiated') {
      return 'fas fa-hourglass-start';
    } else if (timeline.Name === 'Litigation Timed Out') {
      return 'far fa-hourglass-end';
    } else if (timeline.Name === 'Litigation Answered') {
      return 'fas fa-hourglass-half';
    } else if (timeline.Name === 'Litigation Failed') {
      return 'fas fa-hourglass';
    } else if (timeline.Name === 'Litigation Passed') {
      return 'fas fa-hourglass';
    } else if (timeline.Name.startsWith('Offer Paidout')) {
      return 'fas fa-money-bill';
    } else if (timeline.Name === 'Data Holder Chosen') {
      return 'fas fa-plus';
    }
    return 'fas fa-terminal';
  }

  getOffer() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/job/detail/' + this.offerId + '?' + (new Date()).getTime();
    const promise = this.http.get<OTOfferDetailModel>(url, { headers: headers });
    return promise;
  }

  ngOnInit() {
    this.isDarkTheme = $('body').hasClass('dark');
    this.route.params.subscribe(params => {
      this.offerId = params.offerId;

      const startTime = new Date();
      this.getOffer().subscribe(data => {
        const endTime = new Date();
        this.OfferModel = data;
        const diff = endTime.getTime() - startTime.getTime();
        let minWait = 0;
        if (diff < 150) {
          minWait = 150 - diff;
        }
        setTimeout(() => {
          this.isLoading = false;
        }, minWait);
      }, err => {
        this.failedLoading = true;
        this.isLoading = false;
      });
    });
  }
}
