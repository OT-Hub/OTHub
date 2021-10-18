import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HoldingTimePerMonthComponent } from './holding-time-per-month.component';

describe('HoldingTimePerMonthComponent', () => {
  let component: HoldingTimePerMonthComponent;
  let fixture: ComponentFixture<HoldingTimePerMonthComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ HoldingTimePerMonthComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(HoldingTimePerMonthComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
