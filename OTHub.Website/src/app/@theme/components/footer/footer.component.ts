import { Component } from '@angular/core';

@Component({
  selector: 'ngx-footer',
  styleUrls: ['./footer.component.scss'],
  template: `
  <div>
  <a href="/system/status">OT Hub Status</a>&nbsp;
  <a href="/system/smartcontracts">Smart Contracts</a>&nbsp;
  <a href="/misc/donations">Donations</a>
  </div>
  `,
})
export class FooterComponent {
}
