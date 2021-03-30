import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StarfleetboardingComponent } from './starfleetboarding.component';

describe('StarfleetboardingComponent', () => {
  let component: StarfleetboardingComponent;
  let fixture: ComponentFixture<StarfleetboardingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ StarfleetboardingComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(StarfleetboardingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
