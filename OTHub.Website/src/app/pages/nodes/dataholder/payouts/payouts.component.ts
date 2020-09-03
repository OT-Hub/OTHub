import { Component, OnInit, Input } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'ngx-payouts',
  templateUrl: './payouts.component.html',
  styleUrls: ['./payouts.component.scss']
})
export class PayoutsComponent implements OnInit {

  constructor(private router: Router,) { }

  @Input('identity') identity: string; 

  ngOnInit(): void {
  }

  ViewPayoutsInUSD() {
    this.router.navigateByUrl('/nodes/dataholders/' + this.identity + '/report/usd');
  }

}
