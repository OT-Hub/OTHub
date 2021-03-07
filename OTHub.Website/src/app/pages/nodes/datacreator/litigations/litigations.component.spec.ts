import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { LitigationsComponent } from './litigations.component';

describe('LitigationsComponent', () => {
  let component: LitigationsComponent;
  let fixture: ComponentFixture<LitigationsComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ LitigationsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LitigationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
