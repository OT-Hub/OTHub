import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UnstakeboardingComponent } from './unstakeboarding.component';

describe('UnstakeboardingComponent', () => {
  let component: UnstakeboardingComponent;
  let fixture: ComponentFixture<UnstakeboardingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ UnstakeboardingComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(UnstakeboardingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
