import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { OffersDetailComponent } from './offersdetail.component';

describe('OffersDetailComponent', () => {
  let component: OffersDetailComponent;
  let fixture: ComponentFixture<OffersDetailComponent>;

  beforeEach(async(() => {
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
