import { NbMenuItem } from '@nebular/theme';

export const MENU_ITEMS: NbMenuItem[] = [
  // {
  //   title: 'Starfleet Boarding',
  //   icon: 'flash-outline',
  //   link: '/starfleetboarding',
  //   home: true,
  // },
  {
    title: 'Dashboard',
    icon: 'home-outline',
    link: '/dashboard',
    home: true,
  },
  {
    title: 'Jobs',
    icon: 'briefcase-outline',
    link: '/offers/recent'
  },
  {
    title: 'Nodes',
    icon: 'hard-drive-outline',
    children: [
      // {
      //   title: 'My Nodes',
      //   link: '/nodes/mynodes',
      // },
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
  // {
  //   title: 'Misc',
  //   icon: 'umbrella-outline',
  //   children: [
  //     {
  //       title: 'Price Factor Calculator',
  //       link: '/misc/pricefactor/calculator',
  //     },
  //   ],
  // },
  {
    title: 'Global Activity',
    icon: 'monitor-outline',
    link: '/globalactivity'
  },
  // {
  //   title: 'System Status',
  //   icon: 'activity-outline',
  //   link: '/system/status'
  // },
];
