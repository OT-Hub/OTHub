import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { FailedLoadingPageComponent } from './failed-loading-page.component';

describe('FailedLoadingPageComponent', () => {
  let component: FailedLoadingPageComponent;
  let fixture: ComponentFixture<FailedLoadingPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ FailedLoadingPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FailedLoadingPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
