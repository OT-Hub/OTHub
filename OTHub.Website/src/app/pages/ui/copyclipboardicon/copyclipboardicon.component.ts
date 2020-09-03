import { Component, OnInit, Input, HostListener, Output, EventEmitter, Directive } from '@angular/core';
import { NbIconLibraries } from '@nebular/theme';
declare const $: any;
@Component({
  selector: 'app-copyclipboardicon',
  templateUrl: './copyclipboardicon.component.html',
  styleUrls: ['./copyclipboardicon.component.scss']
})
export class CopyclipboardiconComponent implements OnInit {

  constructor(iconsLibrary: NbIconLibraries) {
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
    $.notify({
      message: 'Text copied to clipboard.'
  },
      {
          type: 'bg-blue',
          allow_dismiss: true,
          newest_on_top: true,
          timer: 1000,
          placement: {
              from: 'top',
              align: 'left'
          },
          animate: {
              enter: 'animated fadeInLeft',
              exit: 'animated fadeOutLeft'
          },
          template: '<div data-notify="container" class="bootstrap-notify-container alert alert-dismissible {0} ' + (true ? "p-r-35" : "") + '" role="alert">' +
              '<button type="button" aria-hidden="true" class="close" data-notify="dismiss">×</button>' +
              '<span data-notify="icon"></span> ' +
              '<span data-notify="title">{1}</span> ' +
              '<span data-notify="message">{2}</span>' +
              '<div class="progress" data-notify="progressbar">' +
              '<div class="progress-bar progress-bar-{0}" role="progressbar" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100" style="width: 0%;"></div>' +
              '</div>' +
              '<a href="{3}" target="{4}" data-notify="url"></a>' +
              '</div>'
      });
 }
}
