import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-findbymanagementwallet',
  templateUrl: './findbymanagementwallet.component.html',
  styleUrls: ['./findbymanagementwallet.component.scss']
})
export class FindbymanagementwalletComponent implements OnInit {

  address: string;

  constructor( private route: ActivatedRoute, private router: Router) { }

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.address = params.address;
    });
  }

  afterDataHoldersLoad(count: number) {

  }
}
