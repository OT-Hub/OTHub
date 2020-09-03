import { Component, OnInit, ChangeDetectorRef, OnDestroy, NgZone } from '@angular/core';

import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
declare const $: any;
declare const noUiSlider: any;
import * as moment from 'moment';
import { MyNodeService } from '../../nodes/mynodeservice';
import { HubHttpService } from '../../hub-http-service';


export interface Weather {
  origintrail: Origintrail;
}
export interface Origintrail {
  eth: number;
}


@Component({
  selector: 'app-pricefactorcalculator',
  templateUrl: './pricefactorcalculator.component.html',
  styleUrls: ['./pricefactorcalculator.component.scss']
})
export class PriceFactorCalculatorComponent implements OnInit, OnDestroy {
  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router,
    public myNodeService: MyNodeService, private httpService: HubHttpService) {
    this.isLoading = true;
    this.failedLoading = false;
    this.IsTestNet = httpService.IsTestNet;
  }
  failedLoading: boolean;
  IsTestNet: boolean;
  isLoading: boolean;
  isDarkTheme: boolean;

  priceFactor: number;
  holdingTime: number;
  datasetSize: number;
  tracePriceInEth: number;
  GetDataObservable: any;

  ngOnInit() {

    this.GetDataObservable = this.getTracPrice().subscribe(data => {
      this.tracePriceInEth = data.origintrail.eth;

      $('#tracPriceInEth').val(this.tracePriceInEth);

      $(document).ready(() => {
        this.setupPriceFactor();
        this.setupDataSetFactor();
        this.setupHoldingTimeFactor();

        this.failedLoading = false;
        this.isLoading = false;
      });
    }, err => {
      this.failedLoading = true;
      this.isLoading = false;
    });
  }

  getTracPrice() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');

    const coinGeckoLink = 'https://api.coingecko.com/api/v3/simple/price?ids=origintrail&vs_currencies=eth';
    return this.http.get<Weather>(coinGeckoLink, { headers });
  }

  setupDataSetFactor() {
    const slider = document.getElementById('dataSetSlider');

    const sliderJs = noUiSlider.create(slider, {
      start: 0.1,
      step: 0.1,
      orientation: 'horizontal',
      connect: true,
      range: {
        'min': [0.1],
        'max': [10]
      },
      pips: { mode: 'count', values: 6 }
    });

    sliderJs.on('update', (values, handle) => {
      this.datasetSize = values[handle];

      $('.dataSetSlider-val').text(this.datasetSize + ' MB');
      this.updatePrice();
    });

  }

  setupHoldingTimeFactor() {
    const slider = document.getElementById('holdingTimeSlider');

    const sliderJs = noUiSlider.create(slider, {
      start: 182,
      step: 1,
      orientation: 'horizontal',
      connect: true,
      range: {
        'min': [1],
        'max': [365]
      },
      pips: { mode: 'count', values: 13 }
    });

    sliderJs.on('update', (values, handle) => {
      this.holdingTime = values[handle];

      $('.holdingTimeSlider-val').text(this.holdingTime + ' Days');
      this.updatePrice();
    });

  }

  setupPriceFactor() {
    const slider = document.getElementById('priceFactorSlider');

    const sliderJs = noUiSlider.create(slider, {
      start: 5,
      step: 0.2,
      orientation: 'horizontal',
      connect: true,
      range: {
        'min': [0.1],
        'max': [10]
      },
      pips: { mode: 'count', values: 6 }
    });

    sliderJs.on('update', (values, handle) => {
      this.priceFactor  = values[handle];

      $('.priceFactorSlider-val').text(this.priceFactor);
      this.updatePrice();
    });

  }

  updatePrice() {
    const amount = Math.round(2 * (0.00075 / this.tracePriceInEth) + this.priceFactor * Math.sqrt(2 * this.holdingTime * this.datasetSize));
    $('.trac-val').text(amount + ' TRAC');
  }

  ngOnDestroy() {
    this.GetDataObservable.unsubscribe();
  }
}
