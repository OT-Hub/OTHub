import { Component, OnInit, Input, HostListener, Output, EventEmitter, Directive } from '@angular/core';
import { NbIconLibraries, NbToastrService } from '@nebular/theme';
declare const $: any;
@Component({
  selector: 'app-copyclipboardicon',
  templateUrl: './copyclipboardicon.component.html',
  styleUrls: ['./copyclipboardicon.component.scss']
})
export class CopyclipboardiconComponent implements OnInit {

  constructor(iconsLibrary: NbIconLibraries, private toastrService: NbToastrService) {
    iconsLibrary.registerFontPack('fa', { packClass: 'fa', iconClassPrefix: 'fa' });
   }

  @Input() textToCopy: string;

  ngOnInit() {
  }

  public onClick(event: MouseEvent): void {
    event.preventDefault();
    if (!this.textToCopy) {
      return;
    }

    const listener = (e: ClipboardEvent) => {
      const clipboard = e.clipboardData;
      clipboard.setData('text', this.textToCopy.toString());
      e.preventDefault();

      this.notify();
    };

    document.addEventListener('copy', listener, false);
    document.execCommand('copy');
    document.removeEventListener('copy', listener, false);
  }

  public notify() {

    this.toastrService.show(
      status || 'Success',
      `Text copied to clipboard`);
 }
}
