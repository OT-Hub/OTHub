import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { OffersDetailComponent } from './offersdetail.component';

describe('OffersDetailComponent', () => {
  let component: OffersDetailComponent;
  let fixture: ComponentFixture<OffersDetailComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [OffersDetailComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(OffersDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
