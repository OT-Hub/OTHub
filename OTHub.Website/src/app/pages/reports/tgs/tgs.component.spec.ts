import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TGSComponent } from './tgs.component';

describe('TGSComponent', () => {
  let component: TGSComponent;
  let fixture: ComponentFixture<TGSComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ TGSComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(TGSComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
