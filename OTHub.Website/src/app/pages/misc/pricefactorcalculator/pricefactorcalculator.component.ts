import { Component, OnInit, ChangeDetectorRef, OnDestroy, NgZone } from '@angular/core';

import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
declare const $: any;
declare const noUiSlider: any;
import * as moment from 'moment';
import { HubHttpService } from '../../hub-http-service';

export interface OrigintrailPriceEthWrapper {
  origintrail: OrigintrailPriceEth;
}
export interface OrigintrailPriceEth {
  eth: number;
}

export interface OrigintrailPriceUsdWrapper {
  origintrail: OrigintrailPriceUsd;
}
export interface OrigintrailPriceUsd {
  usd: number;
}


@Component({
  selector: 'app-pricefactorcalculator',
  templateUrl: './pricefactorcalculator.component.html',
  styleUrls: ['./pricefactorcalculator.component.scss']
})
export class PriceFactorCalculatorComponent implements OnInit, OnDestroy {
  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router,
    private httpService: HubHttpService) {
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
  tracePriceInUsd: number;
  GetDataObservableEth: any;
  GetDataObservableUsd: any;

  ngOnInit() {

    this.GetDataObservableEth = this.getTracPriceInEth().subscribe(data => {
      this.tracePriceInEth = data.origintrail.eth;
      
      this.GetDataObservableUsd = this.getTracPriceInUsd().subscribe(data => {
        this.tracePriceInUsd = data.origintrail.usd;

        $(document).ready(() => {
          this.setupPriceFactor();
          this.setupDataSetFactor();
          this.setupHoldingTimeFactor();
  
          this.failedLoading = false;
          this.isLoading = false;

          $('#tracPriceInEth').val(this.tracePriceInEth);
        });
       });


    }, err => {
      this.failedLoading = true;
      this.isLoading = false;
    });
  }

  getTracPriceInEth() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');

    const coinGeckoLink = 'https://api.coingecko.com/api/v3/simple/price?ids=origintrail&vs_currencies=eth';
    return this.http.get<OrigintrailPriceEthWrapper>(coinGeckoLink, { headers });
  }

  getTracPriceInUsd() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');

    const coinGeckoLink = 'https://api.coingecko.com/api/v3/simple/price?ids=origintrail&vs_currencies=usd';
    return this.http.get<OrigintrailPriceUsdWrapper>(coinGeckoLink, { headers });
  }

  setupDataSetFactor() {
    const slider = document.getElementById('dataSetSlider');


    const sliderJs = noUiSlider.create(slider, {
      start: 0.5,
      step: 0.1,
      orientation: 'horizontal',
      connect: true,
      range: {
        'min': [0.1],
        'max': [10]
      },
      pips: { mode: 'count', values: 10 }
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
        'max': [1825]
      },
      pips: { mode: 'count', values: 8 }
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
        'max': [15]
      },
      pips: { mode: 'count', values: 15 }
    });

    sliderJs.on('update', (values, handle) => {
      this.priceFactor  = values[handle];

      $('.priceFactorSlider-val').text(this.priceFactor);
      this.updatePrice();
    });

  }

  updatePrice() {
    const tracAmount = Math.round(2 * (0.00075 / this.tracePriceInEth) + this.priceFactor * Math.sqrt(2 * this.holdingTime * this.datasetSize));
    $('.trac-eth-val').text(tracAmount + ' TRAC');
    $('.trac-usd-val').text('$' + (tracAmount * this.tracePriceInUsd).toFixed(2));
  }

  ngOnDestroy() {
    this.GetDataObservableEth?.unsubscribe();
    this.GetDataObservableUsd?.unsubscribe();
  }
}
