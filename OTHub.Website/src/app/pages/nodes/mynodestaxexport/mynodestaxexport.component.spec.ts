import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MynodestaxexportComponent } from './mynodestaxexport.component';

describe('MynodestaxexportComponent', () => {
  let component: MynodestaxexportComponent;
  let fixture: ComponentFixture<MynodestaxexportComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MynodestaxexportComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(MynodestaxexportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
