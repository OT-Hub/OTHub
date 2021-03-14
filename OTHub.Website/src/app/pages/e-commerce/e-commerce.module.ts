import { NgModule } from '@angular/core';
import { ECommerceComponent } from './e-commerce.component';
import { AuthButtonComponent } from '../miscellaneous/AuthButtonComponent';
import { CommonModule, DecimalPipe } from '@angular/common';
import {MatCardModule} from '@angular/material/card';
import {MatIconModule} from '@angular/material/icon';
import { FlexLayoutModule } from '@angular/flex-layout';
@NgModule({
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    FlexLayoutModule
  ],
  declarations: [
    ECommerceComponent,
    AuthButtonComponent
  ],
  providers: []
})
export class ECommerceModule { }
