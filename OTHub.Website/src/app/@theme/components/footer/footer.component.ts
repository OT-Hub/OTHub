import { Component, OnDestroy, OnInit } from '@angular/core';
import { NbThemeService } from '@nebular/theme';
import { filter, map, takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';

@Component({
  selector: 'ngx-footer',
  styleUrls: ['./footer.component.scss'],
  templateUrl: './footer.component.html',
})
export class FooterComponent  implements OnInit, OnDestroy {
  // themes = [
  //   {
  //     value: 'default',
  //     name: 'Light',
  //   },
  //   {
  //     value: 'dark',
  //     name: 'Dark',
  //   },
  //   // {
  //   //   value: 'cosmic',
  //   //   name: 'Cosmic',
  //   // },
  //   // {
  //   //   value: 'corporate',
  //   //   name: 'Corporate',
  //   // },
  // ];
  private destroy$: Subject<void> = new Subject<void>();

  constructor(private themeService: NbThemeService) {
    
  } 

  currentTheme = 'default';

  toggleTheme() {
    if (this.currentTheme == 'default') {
      this.changeTheme('dark');
    } else {
      this.changeTheme('default');
    }
  }

  isLight() : boolean {
    return this.currentTheme == 'default';
  }

  
  changeTheme(themeName: string) {
    this.themeService.changeTheme(themeName);
  }

  ngOnInit() {
    this.currentTheme = this.themeService.currentTheme;


    this.themeService.onThemeChange()
      .pipe(
        map(({ name }) => name),
        takeUntil(this.destroy$),
      )
      .subscribe(themeName => this.currentTheme = themeName);
   }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
