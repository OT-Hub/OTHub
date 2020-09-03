import { NbMenuItem } from '@nebular/theme';

export const MENU_ITEMS: NbMenuItem[] = [
  {
    title: 'Dashboard',
    icon: 'home-outline',
    link: '/',
    home: true,
  },
  {
    title: 'Jobs',
    icon: 'briefcase-outline',
    link: '/offers'
  },
  {
    title: 'Nodes',
    icon: 'hard-drive-outline',
    children: [
      {
        title: 'My Nodes',
        link: '/nodes/mynodes',
      },
      {
        title: 'Data Holders',
        link: '/nodes/dataholders',
      },
      {
        title: 'Data Creators',
        link: '/nodes/datacreators',
      }
    ],
  },
  {
    title: 'Misc',
    icon: 'umbrella-outline',
    children: [
      {
        title: 'Price Factor Calculator',
        link: '/misc/pricefactor/calculator',
      },
    ],
  },
  {
    title: 'Global Activity',
    icon: 'monitor-outline',
    link: '/globalactivity'
  },
  {
    title: 'System Status',
    icon: 'activity-outline',
    link: '/system/status'
  },
  // {
  //   title: 'E-commerce',
  //   icon: 'shopping-cart-outline',
  //   link: '/dashboard',
  //   home: true,
  // },
  // {
  //   title: 'IoT Dashboard',
  //   icon: 'home-outline',
  //   link: '/iot-dashboard',
  // },
  // {
  //   title: 'FEATURES',
  //   group: true,
  // },
  // {
  //   title: 'Layout',
  //   icon: 'layout-outline',
  //   children: [
  //     {
  //       title: 'Stepper',
  //       link: '/layout/stepper',
  //     },
  //     {
  //       title: 'List',
  //       link: '/layout/list',
  //     },
  //     {
  //       title: 'Infinite List',
  //       link: '/layout/infinite-list',
  //     },
  //     {
  //       title: 'Accordion',
  //       link: '/layout/accordion',
  //     },
  //     {
  //       title: 'Tabs',
  //       pathMatch: 'prefix',
  //       link: '/layout/tabs',
  //     },
  //   ],
  // },
  // {
  //   title: 'Forms',
  //   icon: 'edit-2-outline',
  //   children: [
  //     {
  //       title: 'Form Inputs',
  //       link: '/forms/inputs',
  //     },
  //     {
  //       title: 'Form Layouts',
  //       link: '/forms/layouts',
  //     },
  //     {
  //       title: 'Buttons',
  //       link: '/forms/buttons',
  //     },
  //     {
  //       title: 'Datepicker',
  //       link: '/forms/datepicker',
  //     },
  //   ],
  // },
  // {
  //   title: 'UI Features',
  //   icon: 'keypad-outline',
  //   link: '/ui-features',
  //   children: [
  //     {
  //       title: 'Grid',
  //       link: '/ui-features/grid',
  //     },
  //     {
  //       title: 'Icons',
  //       link: '/ui-features/icons',
  //     },
  //     {
  //       title: 'Typography',
  //       link: '/ui-features/typography',
  //     },
  //     {
  //       title: 'Animated Searches',
  //       link: '/ui-features/search-fields',
  //     },
  //   ],
  // },
  // {
  //   title: 'Modal & Overlays',
  //   icon: 'browser-outline',
  //   children: [
  //     {
  //       title: 'Dialog',
  //       link: '/modal-overlays/dialog',
  //     },
  //     {
  //       title: 'Window',
  //       link: '/modal-overlays/window',
  //     },
  //     {
  //       title: 'Popover',
  //       link: '/modal-overlays/popover',
  //     },
  //     {
  //       title: 'Toastr',
  //       link: '/modal-overlays/toastr',
  //     },
  //     {
  //       title: 'Tooltip',
  //       link: '/modal-overlays/tooltip',
  //     },
  //   ],
  // },
  // {
  //   title: 'Extra Components',
  //   icon: 'message-circle-outline',
  //   children: [
  //     {
  //       title: 'Calendar',
  //       link: '/extra-components/calendar',
  //     },
  //     {
  //       title: 'Progress Bar',
  //       link: '/extra-components/progress-bar',
  //     },
  //     {
  //       title: 'Spinner',
  //       link: '/extra-components/spinner',
  //     },
  //     {
  //       title: 'Alert',
  //       link: '/extra-components/alert',
  //     },
  //     {
  //       title: 'Calendar Kit',
  //       link: '/extra-components/calendar-kit',
  //     },
  //     {
  //       title: 'Chat',
  //       link: '/extra-components/chat',
  //     },
  //   ],
  // },
  // {
  //   title: 'Maps',
  //   icon: 'map-outline',
  //   children: [
  //     {
  //       title: 'Google Maps',
  //       link: '/maps/gmaps',
  //     },
  //     {
  //       title: 'Leaflet Maps',
  //       link: '/maps/leaflet',
  //     },
  //     {
  //       title: 'Bubble Maps',
  //       link: '/maps/bubble',
  //     },
  //     {
  //       title: 'Search Maps',
  //       link: '/maps/searchmap',
  //     },
  //   ],
  // },
  // {
  //   title: 'Charts',
  //   icon: 'pie-chart-outline',
  //   children: [
  //     {
  //       title: 'Echarts',
  //       link: '/charts/echarts',
  //     },
  //     {
  //       title: 'Charts.js',
  //       link: '/charts/chartjs',
  //     },
  //     {
  //       title: 'D3',
  //       link: '/charts/d3',
  //     },
  //   ],
  // },
  // {
  //   title: 'Editors',
  //   icon: 'text-outline',
  //   children: [
  //     {
  //       title: 'TinyMCE',
  //       link: '/editors/tinymce',
  //     },
  //     {
  //       title: 'CKEditor',
  //       link: '/editors/ckeditor',
  //     },
  //   ],
  // },
  // {
  //   title: 'Tables & Data',
  //   icon: 'grid-outline',
  //   children: [
  //     {
  //       title: 'Smart Table',
  //       link: '/tables/smart-table',
  //     },
  //     {
  //       title: 'Tree Grid',
  //       link: '/tables/tree-grid',
  //     },
  //   ],
  // },
  // {
  //   title: 'Miscellaneous',
  //   icon: 'shuffle-2-outline',
  //   children: [
  //     {
  //       title: '404',
  //       link: '/miscellaneous/404',
  //     },
  //   ],
  // },
  // {
  //   title: 'Auth',
  //   icon: 'lock-outline',
  //   children: [
  //     {
  //       title: 'Login',
  //       link: '/auth/login',
  //     },
  //     {
  //       title: 'Register',
  //       link: '/auth/register',
  //     },
  //     {
  //       title: 'Request Password',
  //       link: '/auth/request-password',
  //     },
  //     {
  //       title: 'Reset Password',
  //       link: '/auth/reset-password',
  //     },
  //   ],
  // },
];
