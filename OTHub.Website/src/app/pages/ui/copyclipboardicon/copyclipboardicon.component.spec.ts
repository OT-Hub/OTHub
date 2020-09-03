import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CopyclipboardiconComponent } from './copyclipboardicon.component';

describe('CopyclipboardiconComponent', () => {
  let component: CopyclipboardiconComponent;
  let fixture: ComponentFixture<CopyclipboardiconComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CopyclipboardiconComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CopyclipboardiconComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
