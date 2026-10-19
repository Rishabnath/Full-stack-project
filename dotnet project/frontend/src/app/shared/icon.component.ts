import { Component, Input } from '@angular/core';

@Component({
  selector: 'fs-icon',
  standalone: true,
  template: `<svg viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path [attr.d]="path" /></svg>`,
  styles: [`:host{display:inline-flex;width:20px;height:20px;flex:0 0 auto}svg{width:100%;height:100%}`]
})
export class IconComponent {
  @Input({ required: true }) name = 'sparkles';
  get path(): string { return this.paths[this.name] || this.paths['sparkles']; }
  readonly paths: Record<string, string> = {
    dashboard: 'M3 3h7v8H3z M14 3h7v5h-7z M14 12h7v9h-7z M3 15h7v6H3z',
    market: 'M3 10h18l-1.5 10h-15z M5 10l2-6h10l2 6 M9 14v3 M15 14v3',
    orders: 'M8 6h13 M8 12h13 M8 18h13 M3 6h.01 M3 12h.01 M3 18h.01',
    manage: 'M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8z M19.4 15a1.7 1.7 0 0 0 .34 1.88l.06.06-1.7 2.94-.08-.02a1.7 1.7 0 0 0-1.8.66l-.05.07h-3.4l-.04-.08a1.7 1.7 0 0 0-1.53-1.04 1.7 1.7 0 0 0-.91.27l-.07.04-2.94-1.7.02-.09a1.7 1.7 0 0 0-.66-1.8l-.07-.04v-3.4l.08-.05a1.7 1.7 0 0 0 .77-1.41 1.7 1.7 0 0 0-.27-.91l-.04-.07 1.7-2.94.09.02a1.7 1.7 0 0 0 1.8-.66l.04-.07h3.4l.05.08a1.7 1.7 0 0 0 1.41.77 1.7 1.7 0 0 0 .91-.27l.07-.04 2.94 1.7-.02.09a1.7 1.7 0 0 0 .66 1.8l.07.04v3.4z',
    addresses: 'M20 10c0 5-8 11-8 11S4 15 4 10a8 8 0 1 1 16 0z M12 10a2.5 2.5 0 1 0 0-.01z',
    apply: 'M12 3v18 M3 12h18 M5 5l14 14 M19 5 5 19',
    sessions: 'M12 8v4l3 2 M21 12a9 9 0 1 1-2.64-6.36 M21 3v6h-6',
    search: 'm20 20-4.2-4.2 M17 10.5a6.5 6.5 0 1 1-13 0 6.5 6.5 0 0 1 13 0z',
    bell: 'M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9 M10 21h4',
    chevron: 'm9 18 6-6-6-6',
    collapse: 'M14 4h6v16h-6 M4 12h10 M9 7l-5 5 5 5',
    menu: 'M3 6h18 M3 12h18 M3 18h18',
    close: 'M18 6 6 18 M6 6l12 12',
    logout: 'M10 17l5-5-5-5 M15 12H3 M12 3h6a3 3 0 0 1 3 3v12a3 3 0 0 1-3 3h-6',
    plus: 'M12 5v14 M5 12h14',
    edit: 'm15 5 4 4 M4 20l4-.8L20 7.2a2.1 2.1 0 0 0-3-3L5 16.2z',
    trash: 'M3 6h18 M8 6V4h8v2 M19 6l-1 14H6L5 6 M10 11v5 M14 11v5',
    check: 'm5 12 4 4L19 6',
    arrow: 'M5 12h14 M13 6l6 6-6 6',
    clock: 'M12 8v4l2.5 1.5 M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z',
    sparkles: 'm12 3 1.6 5.4L19 10l-5.4 1.6L12 17l-1.6-5.4L5 10l5.4-1.6z M19 14l.9 2.1L22 17l-2.1.9L19 20l-.9-2.1L16 17l2.1-.9z',
    bag: 'M5 8h14l1 13H4L5 8z M9 8a3 3 0 1 1 6 0',
    pin: 'M20 10c0 5-8 11-8 11S4 15 4 10a8 8 0 1 1 16 0z M12 10a2.5 2.5 0 1 0 0-.01z',
    chart: 'M3 3v18h18 M8 16v-4 M13 16V6 M18 16V9',
    users: 'M16 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2 M10 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z M20 21v-2a4 4 0 0 0-3-3.87 M16 3.13a4 4 0 0 1 0 7.75',
    filter: 'M4 7h16 M7 12h10 M10 17h4',
    chevrondown: 'm6 9 6 6 6-6',
    star: 'm12 3 2.8 5.7 6.2.9-4.5 4.4 1.1 6.2L12 17.3l-5.6 2.9 1.1-6.2L3 9.6l6.2-.9z',
    phone: 'M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.4 19.4 0 0 1-6-6A19.8 19.8 0 0 1 2.1 4.2 2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7l.4 2.8a2 2 0 0 1-.6 1.7L7.6 9.5a16 16 0 0 0 6 6l1.3-1.3a2 2 0 0 1 1.7-.6l2.8.4a2 2 0 0 1 1.6 1.9z'
  };
}
