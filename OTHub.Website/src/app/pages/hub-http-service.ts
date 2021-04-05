import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class HubHttpService {
    /**
     *
     */
    constructor() {
        if (window.location.hostname) {
            if (window.location.hostname === 'localhost') {
                 this.ApiUrl = 'http://localhost:5000';
                // this.ApiUrl = 'https://testnetapi.othub.info';
                 this.IsTestNet = false;
            } else if (window.location.hostname === 'othub-testnet.origin-trail.network') {
                this.ApiUrl = 'https://testnet-api.othub.info';
                this.IsTestNet = true;
            } else if (window.location.hostname === 'v5.othub.info') {
                this.ApiUrl = 'https://v5api.othub.info';
                this.IsTestNet = false;
            }
        }
    }

    public IsTestNet = false;

         public ApiUrl = 'https://othub-api.origin-trail.network';
         //public ApiUrl = 'http://localhost:6851';
}
