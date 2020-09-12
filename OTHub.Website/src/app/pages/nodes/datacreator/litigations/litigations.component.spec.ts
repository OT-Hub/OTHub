import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { LitigationsComponent } from './litigations.component';

describe('LitigationsComponent', () => {
  let component: LitigationsComponent;
  let fixture: ComponentFixture<LitigationsComponent>;

  beforeEach(async(() => {
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
