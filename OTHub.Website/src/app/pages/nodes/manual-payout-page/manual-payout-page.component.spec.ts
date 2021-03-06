import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ManualPayoutPageComponent } from './manual-payout-page.component';

describe('ManualPayoutPageComponent', () => {
  let component: ManualPayoutPageComponent;
  let fixture: ComponentFixture<ManualPayoutPageComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ManualPayoutPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ManualPayoutPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
