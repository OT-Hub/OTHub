import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RpcComponent } from './rpc.component';

describe('RpcComponent', () => {
  let component: RpcComponent;
  let fixture: ComponentFixture<RpcComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ RpcComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(RpcComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
