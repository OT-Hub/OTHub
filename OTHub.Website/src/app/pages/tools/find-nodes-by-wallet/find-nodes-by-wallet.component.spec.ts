import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FindNodesByWalletComponent } from './find-nodes-by-wallet.component';

describe('FindNodesByWalletComponent', () => {
  let component: FindNodesByWalletComponent;
  let fixture: ComponentFixture<FindNodesByWalletComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ FindNodesByWalletComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(FindNodesByWalletComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
