import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StakedTokensByDayComponent } from './staked-tokens-by-day.component';

describe('StakedTokensByDayComponent', () => {
  let component: StakedTokensByDayComponent;
  let fixture: ComponentFixture<StakedTokensByDayComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ StakedTokensByDayComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(StakedTokensByDayComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
